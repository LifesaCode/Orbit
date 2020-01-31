using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Orbit.Models
{
    public class ExternalCoolantLoopData : IAlertableModel, IEquatable<ExternalCoolantLoopData>
    {
        #region Limits

        private const int radiatorRotationUpperLimit = 215;
        private const int radiatorRotationLowerLimit = -215;
        private const int radiatorRotationTolerance = 10;

        private const int fluidPressureUpperLimit = 480;
        private const int fluidPressureLowerLimit = 170;
        private const int fluidPressureTolerance = 30;

        private const double outputFluidTemperatueUpperLimit = 19;
        private const double outputFluidTemperatureLowerLimit = 1;
        private const double outputFluidTemperatureTolerance = 2;

        private int mixValveUpperLimit = 100;
        private int mixValveLowerLimit = 0;
        private int mixValveTolerance = 5;

        private bool radiatorRotationIncreasing = true;

        #endregion Limits

        #region Public Properties

        public string ComponentName => "ExternalCoolantSystem";

        public DateTimeOffset ReportDateTime { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// overall status of the system
        /// </summary>
        public SystemStatus Status { get; set; }

        /// <summary>
        /// position in degrees from neutral point.
        /// </summary>
        [Range(-215, 215)]
        public int RadiatorRotation { get; set; }

        /// <summary>
        /// pump for fluid circuit A
        /// </summary>
        public bool PumpAOn { get; set; }

        /// <summary>
        /// pump for fluid circuit B
        /// </summary>
        public bool PumpBOn { get; set; }

        /// <summary>
        /// position of the valve that mixes 'hot' fluid returning from heat exchanger and 'cold' fluid 
        /// returning from radiator. Acts like a shower temp valve. 0 = all 'hot', 100 = all 'cold'
        /// </summary>
        public int MixValvePosition { get; set; }

        /// <summary>
        /// pressure of fluid in loop A
        /// </summary>
        [Range(0, 600)]
        public int LineAPressure { get; set; }

        /// <summary>
        /// pressure of fluid in loop B
        /// </summary>
        [Range(0, 600)]
        public int LineBPressure { get; set; }

        /// <summary>
        /// heats the external coolant if the heat offload from the station is too low to warm the -40F degree ammonia
        /// to at least 36F to prevent the internal coolant fluid (water) from freezing
        /// </summary>
        public bool LineHeaterOn { get; set; }

        /// <summary>
        /// True if radiator is extended (deployed), false if retracted (not deployed)
        /// </summary>
        public bool RadiatorDeployed { get; set; }

        /// <summary>
        /// temperature of fluid returning from radiator flow control valve to internal/external heat exchanger
        /// </summary>
        [Range(-157, 121)]
        public double OutputFluidTemperature { get; set; }

        /// <summary>
        /// goal output fluid temperature
        /// </summary>
        public double SetTemperature { get; set; }

        #endregion Public Properties

        #region Constructors

        public ExternalCoolantLoopData() { }

        public ExternalCoolantLoopData(ExternalCoolantLoopData other)
        {
            Status = other.Status;
            RadiatorRotation = other.RadiatorRotation;
            PumpAOn = other.PumpAOn;
            PumpBOn = other.PumpBOn;
            MixValvePosition = other.MixValvePosition;
            LineAPressure = other.LineAPressure;
            LineBPressure = other.LineBPressure;
            LineHeaterOn = other.LineHeaterOn;
            RadiatorDeployed = other.RadiatorDeployed;
            OutputFluidTemperature = other.OutputFluidTemperature;
            SetTemperature = other.SetTemperature;
        }

        #endregion Constructors

        #region Methods

        public void ProcessData()
        {
            GenerateData();
            bool troubleFlag = false;

            // pump failure (loss of pump motor rotation, regardless of line pressure)
            if (!PumpAOn || !PumpBOn)
            {
                troubleFlag = true;
            }

            if (OutputFluidTemperature > SetTemperature)
            {
                // open the mixing valve and allow more 'cold' fluid in the mix
                IncreaseFluidMix();
            }
            else if (OutputFluidTemperature < SetTemperature)
            {
                // close the mixing valve to keep more 'hot' fluid in the mix
                DecreaseFluidMix();
            }

            if ((LineAPressure > fluidPressureUpperLimit) || (LineAPressure < fluidPressureLowerLimit))
            {
                // problem in fluid line A (while pump on and working), shut off pumpA to prevent pump damage 
                PumpAOn = false;
                troubleFlag = true;
            }
            else
            {   // many need to make this a manual restart in event presssure restore is due to heat expansion?
                PumpAOn = true;
            }

            if ((LineBPressure > fluidPressureUpperLimit) || (LineBPressure < fluidPressureLowerLimit))
            {
                // problem in fluid line B (while pump on and working), shut off pumpB to prevent pump damage
                PumpBOn = false;
                troubleFlag = true;
            }
            else
            {   // many need to make this a manual restart in event presssure restore is due to heat expansion?
                PumpBOn = true;
            }

            if (RadiatorDeployed)
            {
                // moved beyond bounds
                if ((RadiatorRotation > radiatorRotationUpperLimit) || (RadiatorRotation < radiatorRotationLowerLimit))
                {
                    troubleFlag = true;
                }
                // simulate radiator rotation
                RotateRadiator();
            }

            if (troubleFlag)
            {
                Status = SystemStatus.Trouble;
            }
            else
            {
                Status = SystemStatus.On;
            }
        }

        private void GenerateData()
        {
            Random rand = new Random();

            LineAPressure = rand.Next(0, 600);
            LineBPressure = rand.Next(100, 600);
            OutputFluidTemperature = rand.Next(-600, 800) / 10.0;

            if (rand.Next(0, 10) == 5)
            {
                PumpAOn = !PumpAOn;
            }
            if (rand.Next(0, 10) == 9)
            {
                PumpBOn = !PumpBOn;
            }
        }

        private void IncreaseFluidMix()
        {
            // need formula: if valve opened 1 'degree', how much would temp change, use difference
            // in outflow and set temp to determine amount to change valve

            if (LineHeaterOn)
            {
                // if heater is on, turn it off and recheck temp before moving valve position
                LineHeaterOn = false;
            }
            else
            {
                if (MixValvePosition < mixValveUpperLimit)
                {
                    MixValvePosition++;
                }
            }
        }

        private void DecreaseFluidMix()
        {
            // Sould we use a formula? ex: if valve closed 1 'degree', how much would temp change, 
            // then use that change in outflow and set temp to determine amount to change valve

            if (MixValvePosition == mixValveLowerLimit)
            {
                // heat load from station is too low to keep fluid above min temp, turn heater on
                LineHeaterOn = true;
            }
            else
            {
                MixValvePosition--;
            }
        }

        private void RotateRadiator()
        {
            // rotate radiator back and forth between range bounds
            if (radiatorRotationIncreasing && (RadiatorRotation < radiatorRotationUpperLimit))
            {
                RadiatorRotation++;
            }
            else if (!radiatorRotationIncreasing && (RadiatorRotation > radiatorRotationLowerLimit))
            {
                RadiatorRotation--;
            }
            else
            {
                // reached a bound, switch direction
                radiatorRotationIncreasing = !radiatorRotationIncreasing;
            }
        }

        #endregion Methods

        #region Alert Generation

        private IEnumerable<Alert> CheckPumpA()
        {
            if (!PumpAOn)
            {
                yield return new Alert(nameof(PumpAOn), "External coolant pump A is off", AlertLevel.HighError);
            }
            else
            {
                yield return Alert.Safe(nameof(PumpAOn));
            }
        }

        private IEnumerable<Alert> CheckPumpB()
        {
            if (!PumpBOn)
            {
                yield return new Alert(nameof(PumpBOn), "External coolant pump B is off", AlertLevel.HighError);
            }
            else
            {
                yield return Alert.Safe(nameof(PumpBOn));
            }
        }

        private IEnumerable<Alert> CheckMixValvePosition()
        {
            if (MixValvePosition >= mixValveUpperLimit)
            {
                yield return new Alert(nameof(MixValvePosition), "Mix valve position is at maximum", AlertLevel.HighError);
            }
            else if (MixValvePosition >= (mixValveUpperLimit - mixValveTolerance))
            {
                yield return new Alert(nameof(MixValvePosition), "Mix valve position is approaching maximum", AlertLevel.HighWarning);
            }
            else if (MixValvePosition <= mixValveLowerLimit)
            {
                yield return new Alert(nameof(MixValvePosition), "Mix valve position is at minimum", AlertLevel.LowError);
            }
            else if (MixValvePosition <= (mixValveLowerLimit - mixValveTolerance))
            {
                yield return new Alert(nameof(MixValvePosition), "Mix valve position is approaching minimum", AlertLevel.LowWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(MixValvePosition));
            }
        }

        private IEnumerable<Alert> CheckRadiatorDeployed()
        {
            if (!RadiatorDeployed)
            {
                yield return new Alert(nameof(RadiatorDeployed), "External coolant radiator is retracted", AlertLevel.LowWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(RadiatorDeployed));
            }
        }

        private IEnumerable<Alert> CheckRadiatorRotation()
        {
            if (RadiatorRotation > radiatorRotationUpperLimit)
            {
                yield return new Alert(nameof(RadiatorRotation), "Coolant radiator has exceeded maximum available rotation", AlertLevel.HighError);
            }
            else if (RadiatorRotation >= (radiatorRotationUpperLimit - radiatorRotationTolerance))
            {
                yield return new Alert(nameof(RadiatorRotation), "Coolant radiator has exceeded allowed rotation", AlertLevel.HighWarning);
            }
            else if (RadiatorRotation < radiatorRotationLowerLimit)
            {
                yield return new Alert(nameof(RadiatorRotation), "Coolant radiator has exceeded maximum available rotation", AlertLevel.LowError);
            }
            else if (RadiatorRotation <= (radiatorRotationLowerLimit + radiatorRotationTolerance))
            {
                yield return new Alert(nameof(RadiatorRotation), "Coolant radiator has exceeded allowed rotation", AlertLevel.LowWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(RadiatorRotation));
            }
        }

        private IEnumerable<Alert> CheckLineAPressure()
        {
            if (LineAPressure >= fluidPressureUpperLimit)
            {
                yield return new Alert(nameof(LineAPressure), "Coolant line A pressure is above maximum", AlertLevel.HighError);
            }
            else if (LineAPressure >= (fluidPressureUpperLimit - fluidPressureTolerance))
            {
                yield return new Alert(nameof(LineAPressure), "Coolant line A pressure is too high", AlertLevel.HighWarning);
            }
            else if (LineAPressure < fluidPressureLowerLimit)
            {
                yield return new Alert(nameof(LineAPressure), "Coolant line A pressure is below minimum", AlertLevel.LowError);
            }
            else if (LineAPressure <= (fluidPressureLowerLimit + fluidPressureTolerance))
            {
                yield return new Alert(nameof(LineAPressure), "Coolant line A pressure is too low", AlertLevel.LowWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(LineAPressure));
            }
        }

        private IEnumerable<Alert> CheckLineBPressure()
        {
            if (LineBPressure >= fluidPressureUpperLimit)
            {
                yield return new Alert(nameof(LineBPressure), "Coolant line B pressure is above maximum", AlertLevel.HighError);
            }
            else if (LineBPressure >= (fluidPressureUpperLimit - fluidPressureTolerance))
            {
                yield return new Alert(nameof(LineBPressure), "Coolant line B  pressure is too high", AlertLevel.HighWarning);
            }
            else if (LineBPressure < fluidPressureLowerLimit)
            {
                yield return new Alert(nameof(LineBPressure), "Coolant line B pressure is below minimum", AlertLevel.LowError);
            }
            else if (LineBPressure <= (fluidPressureLowerLimit + fluidPressureTolerance))
            {
                yield return new Alert(nameof(LineBPressure), "Coolant line B pressure is too low", AlertLevel.LowWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(LineBPressure));
            }
        }

        private IEnumerable<Alert> CheckOutputFluidTemp()
        {
            if (OutputFluidTemperature >= outputFluidTemperatueUpperLimit)
            {
                yield return new Alert(nameof(OutputFluidTemperature), "External coolant temperature is above maximum", AlertLevel.HighError);
            }
            else if (OutputFluidTemperature >= (outputFluidTemperatueUpperLimit - outputFluidTemperatureTolerance))
            {
                yield return new Alert(nameof(OutputFluidTemperature), "External coolant temperature is too high", AlertLevel.HighWarning);
            }
            else if (OutputFluidTemperature <= outputFluidTemperatureLowerLimit)
            {
                yield return new Alert(nameof(OutputFluidTemperature), "External coolant temperature is below minimum", AlertLevel.LowError);
            }
            else if (OutputFluidTemperature <= (outputFluidTemperatureLowerLimit + outputFluidTemperatureTolerance))
            {
                yield return new Alert(nameof(OutputFluidTemperature), "External coolant temperature is low", AlertLevel.LowWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(OutputFluidTemperature));
            }
        }

        IEnumerable<Alert> IAlertableModel.GenerateAlerts()
        {
            return this.CheckPumpA()
                .Concat(CheckPumpB())
                .Concat(CheckMixValvePosition())
                .Concat(CheckRadiatorDeployed())
                .Concat(CheckRadiatorRotation())
                .Concat(CheckLineAPressure())
                .Concat(CheckLineBPressure())
                .Concat(CheckOutputFluidTemp());
        }

        #endregion Alert generation

        #region Equality Members

        public bool Equals(ExternalCoolantLoopData other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return this.ReportDateTime.Equals(other.ReportDateTime)
                && this.Status == other.Status
                && this.RadiatorRotation == other.RadiatorRotation
                && this.PumpAOn == other.PumpAOn
                && this.PumpBOn == other.PumpBOn
                && this.MixValvePosition == other.MixValvePosition
                && this.LineAPressure == other.LineAPressure
                && this.LineBPressure == other.LineBPressure
                && this.LineHeaterOn == other.LineHeaterOn
                && this.RadiatorDeployed == other.RadiatorDeployed
                && this.OutputFluidTemperature == other.OutputFluidTemperature
                && this.SetTemperature == other.SetTemperature;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is ExternalCoolantLoopData other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                this.ReportDateTime,
                this.Status,
                this.RadiatorRotation,
                this.PumpAOn,
                this.PumpBOn,
                this.MixValvePosition,
                this.LineAPressure,
                (this.LineBPressure,
                    this.LineHeaterOn,
                    this.RadiatorDeployed,
                    this.OutputFluidTemperature,
                    this.SetTemperature)
                );
        }

        public static bool operator ==(ExternalCoolantLoopData left, ExternalCoolantLoopData right) => Equals(left, right);

        public static bool operator !=(ExternalCoolantLoopData left, ExternalCoolantLoopData right) => !Equals(left, right);

        #endregion Equality Members
    }
}