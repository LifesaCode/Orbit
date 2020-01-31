using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Orbit.Models
{
    public class AtmosphereData : IAlertableModel
    {
        #region Limits
        //values in decibels
        private const int cabinAmbientNoiseTolerance = 5;
        private const int cabinAmbientNoiseUpperLimit = 72;

        // values are percentages
        private const double cabinHumidityLevelLowerLimit = 30;
        private const double cabinHumidityLevelTolerance = 10;
        private const double cabinHumidityLevelUpperLimit = 80;

        // values in psia
        private const double cabinPressureLowerLimit = 50;
        private const double cabinPressureTolerance = 5;
        private const double cabinPressureUpperLimit = 110;

        // degrees Celsius
        private const int cabinTemperatureCrewedLowerLimit = 17;
        private const int cabinTemperatureTolerance = 3;

        private const int cabinTemperatureUncrewedLowerLimit = 4;
        private const int cabinTemperatureUpperLimit = 30;

        // values are percentage
        private const int fanSpeedLowerLimit = 20;
        private const int fanSpeedTolerance = 10;
        private const int fanSpeedUpperLimit = 100;

        private int sepertorSpeedUpperLimit = 2400;
        private int seperatorSpeedLowerLimit = 1800;
        private int seperatorTolerance = 200;

        private SystemStatus lastWorkingStatus;
        private double tempControlIncrement = 0.5;

        #endregion Limits

        #region Public Properties

        [NotMapped]
        public string ComponentName => "Cabin Atmosphere";

        public DateTimeOffset ReportDateTime { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// general status of life support and environment as a whole
        /// </summary>
        public Modes CabinStatus { get; set; }

        /// <summary>
        /// Decibel value of cabin noise
        /// </summary>
        [Range(0, 90)]
        public double AmbientNoiseLevel { get; set; }

        /// <summary>
        /// Crewed: 40-75%; uncrewed: 30-80% (is allowed for up to 24hrs while crewed)
        /// </summary>
        [Range(0, 100)]
        public double HumidityLevel { get; set; }

        /// <summary>
        /// desired humidity level set by crew
        /// </summary>
        public double HumiditySetLevel { get; set; }

        /// <summary>
        /// motor speed of the air/liquid seperator in RPM
        /// </summary>
        [Range(0, 2400)]
        public int SeperatorSpeed { get; set; }

        /// <summary>
        /// true when seperator has enough condensate to run, false otherwise
        /// </summary>
        public bool SeperatorFull { get; set; }

        /// <summary>
        /// Pressure used are in kPa and is what is listed as nominal for low EVA modules per interoperability standards
        /// Nominal is 101kpa
        /// </summary>
        [Range(0, 120)]
        public double Pressure { get; set; }

        /// <summary>
        /// the desired cabin pressure
        /// </summary>
        public double SetPressure { get; set; }

        /// <summary>
        /// Ambient air temperature Nominal range is 20-27C uncrewed min is 4C
        /// </summary>
        [Range(-10, 100)]
        public double Temperature { get; set; }

        /// <summary>
        /// desired ambient temperature set by crew
        /// </summary>
        public double SetTemperatureDay { get; set; }

        /// <summary>
        /// desired ambient temperature set by crew
        /// </summary>
        public double SetTemperatureNight { get; set; }

        /// <summary>
        /// position of the heat exchanger air baffles: 
        ///     0 =  all air bypasses heat exhanger/condenser 
        ///     100 = all air passes through heat exchanger/condenser
        /// </summary>
        [Range(0, 100)]
        public int TempControlBafflePosition { get; set; }

        /// <summary>
        /// Air circulation fan speed
        /// </summary>
        [Range(0, 100)]
        public int FanSpeed { get; set; }

        /// <summary>
        /// sensor that will trigger if liquid is detected in air leaving the heat exchanger
        /// </summary>
        public bool LiquidInOutflow { get; set; }

        /// <summary>
        /// redirects outflow air back to heat exchange/condensor if liquid is detected
        /// </summary>
        public DiverterValvePositions ReprocessBafflePosition { get; set; }

        #endregion Public Properties

        #region Constructors

        public AtmosphereData() { }

        public AtmosphereData(AtmosphereData other)
        {
            ReportDateTime = other.ReportDateTime;
            CabinStatus = other.CabinStatus;
            AmbientNoiseLevel = other.AmbientNoiseLevel;
            HumidityLevel = other.HumidityLevel;
            HumiditySetLevel = other.HumiditySetLevel;
            Temperature = other.Temperature;
            SetTemperatureDay = other.SetTemperatureDay;
            SetTemperatureNight = other.SetTemperatureNight;
            ReprocessBafflePosition = other.ReprocessBafflePosition;
            FanSpeed = other.FanSpeed;
            LiquidInOutflow = other.LiquidInOutflow;
            Pressure = other.Pressure;
            SetPressure = other.SetPressure;
            SeperatorSpeed = other.SeperatorSpeed;
            SeperatorFull = other.SeperatorFull;
            TempControlBafflePosition = other.TempControlBafflePosition;
        }

        #endregion Constructors

        #region Methods

        public void ProcessData()
        {
            GenerateData();

            // too hot or humid
            if (Temperature > (SetTemperatureDay + tempControlIncrement)
                || HumidityLevel > HumiditySetLevel) 
            {
                DecreaseTemperature();
            }

            // too cold or dry
            if (CabinStatus == Modes.Crewed)
            {
                if(Temperature < (SetTemperatureDay - tempControlIncrement)
                    || HumidityLevel < HumiditySetLevel) 
                {
                    IncreaseTemperature();
                }
            }
            else if (CabinStatus == Modes.Uncrewed)
            {
                if(Temperature < (cabinTemperatureUncrewedLowerLimit - tempControlIncrement)
                    || HumidityLevel < HumiditySetLevel)
                {
                    IncreaseTemperature();
                }
            }

            if (LiquidInOutflow)
            {
                ReprocessBafflePosition = DiverterValvePositions.Reprocess;
            }
            else
            {
                ReprocessBafflePosition = DiverterValvePositions.Accept;
            }
        }

        private void DecreaseTemperature()
        {
            // open baffle to allow more air across cooling condensor
            if (TempControlBafflePosition < 100)
            {
                TempControlBafflePosition++;
            }
            else
            {
                TempControlBafflePosition = 100;
            }
        }

        private void IncreaseTemperature()
        {
            // close baffle to decrease air across cooling condensor
            if (TempControlBafflePosition > 0)
            {
                TempControlBafflePosition--;
            }
            else
            {
                TempControlBafflePosition = 0;
            }
        }

        private void GenerateData()
        {
            Random rand = new Random();
            Temperature = rand.Next(150, 320) / 10.0;
            HumidityLevel = rand.Next(10, 80);
            Pressure = rand.Next(50, 110);
            AmbientNoiseLevel = rand.Next(0, 72);
            SeperatorSpeed = rand.Next(1000, 3000);

            if(rand.Next(0, 100) % 7 == 0)
            {
                LiquidInOutflow = true;
            }
            else
            {
                LiquidInOutflow = false;
            }
        }

        #endregion Methods

        #region Check Alerts

        private IEnumerable<Alert> CheckCabinAmbientNoiseLevel()
        {
            if (AmbientNoiseLevel > cabinAmbientNoiseUpperLimit)
            {
                yield return new Alert(nameof(AmbientNoiseLevel), "Cabin noise has exceeded maximum", AlertLevel.HighError);
            }
            else if (AmbientNoiseLevel >= (cabinAmbientNoiseUpperLimit - cabinAmbientNoiseTolerance))
            {
                yield return new Alert(nameof(AmbientNoiseLevel), "Cabin noise is elevated", AlertLevel.HighWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(AmbientNoiseLevel));
            }
        }

        private IEnumerable<Alert> CheckHumidityLevel()
        {
            if (HumidityLevel >= cabinHumidityLevelUpperLimit)
            {
                yield return new Alert(nameof(HumidityLevel), "Cabin humidity is above maximum", AlertLevel.HighError);
            }
            else if (HumidityLevel >= (cabinHumidityLevelUpperLimit - cabinHumidityLevelTolerance))
            {
                yield return new Alert(nameof(HumidityLevel), "Humidity is elevated", AlertLevel.HighWarning);
            }
            else if (HumidityLevel <= cabinHumidityLevelLowerLimit)
            {
                yield return new Alert(nameof(HumidityLevel), "Humidity is below minimum", AlertLevel.LowError);
            }
            else if (HumidityLevel <= (cabinHumidityLevelLowerLimit + cabinHumidityLevelTolerance))
            {
                yield return new Alert(nameof(HumidityLevel), "Humidity is low", AlertLevel.LowWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(HumidityLevel));
            }
        }

        private IEnumerable<Alert> CheckPressure()
        {
            if (Pressure >= cabinPressureUpperLimit)
            {
                yield return new Alert(nameof(Pressure), "Cabin pressure has exceeded maximum", AlertLevel.HighError);
            }
            else if (Pressure >= (cabinPressureUpperLimit - cabinPressureTolerance))
            {
                yield return new Alert(nameof(Pressure), "Cabin pressure is elevated", AlertLevel.HighWarning);
            }
            else if (Pressure <= cabinPressureLowerLimit)
            {
                yield return new Alert(nameof(Pressure), "Cabin pressure below minimum", AlertLevel.LowError);
            }
            else if (Pressure < (cabinPressureLowerLimit + cabinPressureTolerance))
            {
                yield return new Alert(nameof(Pressure), "Cabin pressure is low", AlertLevel.LowWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(Pressure));
            }
        }
        private IEnumerable<Alert> CheckCabinTemperature()
        {
            if (Temperature > cabinTemperatureUpperLimit)
            {
                yield return new Alert(nameof(Temperature), "Cabin temperature is above maximum", AlertLevel.HighError);
            }
            else if (Temperature >= (cabinTemperatureUpperLimit - cabinTemperatureTolerance))
            {
                yield return new Alert(nameof(Temperature), "Cabin temperature is high", AlertLevel.HighWarning);
            }
            else if (Temperature < cabinTemperatureCrewedLowerLimit)
            {
                yield return new Alert(nameof(Temperature), "Cabin temperature is below minimum", AlertLevel.LowError);
            }
            else
            {
                yield return Alert.Safe(nameof(Temperature));
            }
        }

        private IEnumerable<Alert> CheckFanSpeed()
        {
            if (FanSpeed > fanSpeedUpperLimit)
            {
                yield return new Alert(nameof(FanSpeed), "Fan speed is above maximum", AlertLevel.HighError);
            }
            else if (FanSpeed >= (fanSpeedUpperLimit - fanSpeedTolerance))
            {
                yield return new Alert(nameof(FanSpeed), "Fan speed is high", AlertLevel.HighWarning);
            }
            else if (FanSpeed < fanSpeedLowerLimit)
            {
                yield return new Alert(nameof(FanSpeed), "Fan speed is below minimum", AlertLevel.LowError);
            }
            else if (FanSpeed <= (fanSpeedLowerLimit - fanSpeedTolerance))
            {
                yield return new Alert(nameof(FanSpeed), "Fan speed is low", AlertLevel.LowWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(FanSpeed));
            }
        }

        private IEnumerable<Alert> CheckSeperator()
        {
            if (SeperatorSpeed > sepertorSpeedUpperLimit)
            {
                yield return new Alert(nameof(SeperatorSpeed), "Seperator speed is above maximum", AlertLevel.HighError);
            }
            else if (SeperatorSpeed > (sepertorSpeedUpperLimit - seperatorTolerance))
            {
                yield return new Alert(nameof(SeperatorSpeed), "Seperator speed is high", AlertLevel.HighWarning);
            }
            else if (SeperatorSpeed < seperatorSpeedLowerLimit)
            {
                yield return new Alert(nameof(SeperatorSpeed), "Seperator speed is below minimum", AlertLevel.LowError);
            }
            else if (SeperatorSpeed < (seperatorSpeedLowerLimit + seperatorTolerance))
            {
                yield return new Alert(nameof(SeperatorSpeed), "Seperator speed is too slow", AlertLevel.LowWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(SeperatorSpeed));
            }
        }

        private IEnumerable<Alert> CheckLiquidInOutflow()
        {
            if (LiquidInOutflow)
            {
                yield return new Alert(nameof(LiquidInOutflow), "Water detected in condensor return flow", AlertLevel.HighWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(LiquidInOutflow));
            }
        }

        IEnumerable<Alert> IAlertableModel.GenerateAlerts()
        {
            return this.CheckPressure()
                .Concat(CheckHumidityLevel())
                .Concat(CheckCabinAmbientNoiseLevel())
                .Concat(CheckCabinTemperature())
                .Concat(CheckFanSpeed())
                .Concat(CheckSeperator())
                .Concat(CheckLiquidInOutflow());
        }

        #endregion Check Alerts

        #region Equality Members

        public bool Equals(AtmosphereData other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return this.ReportDateTime == other.ReportDateTime
                && this.CabinStatus == other.CabinStatus
                && this.AmbientNoiseLevel == other.AmbientNoiseLevel
                && this.HumidityLevel == other.HumidityLevel
                && this.HumiditySetLevel == other.HumiditySetLevel
                && this.Temperature == other.Temperature
                && this.SetTemperatureDay == other.SetTemperatureDay
                && this.SetTemperatureNight == other.SetTemperatureNight
                && this.ReprocessBafflePosition == other.ReprocessBafflePosition
                && this.FanSpeed == other.FanSpeed
                && this.LiquidInOutflow == other.LiquidInOutflow
                && this.Pressure == other.Pressure
                && this.SetPressure == other.SetPressure
                && this.SeperatorSpeed == other.SeperatorSpeed
                && this.SeperatorFull == other.SeperatorFull
                && this.TempControlBafflePosition == other.TempControlBafflePosition;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is AtmosphereData other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                this.ReportDateTime,
                this.CabinStatus,
                this.AmbientNoiseLevel,
                this.HumidityLevel,
                this.HumiditySetLevel,
                this.Temperature,
                this.SetTemperatureDay,
                
                (   this.SetTemperatureNight,
                    this.ReprocessBafflePosition,
                    this.FanSpeed,
                    this.LiquidInOutflow,
                    this.Pressure,
                    this.SetPressure,
                    this.SeperatorSpeed,
                    this.SeperatorFull,
                    this.TempControlBafflePosition
                )
           );
        }

        public static bool operator ==(AtmosphereData left, AtmosphereData right) => Equals(left, right);

        public static bool operator !=(AtmosphereData left, AtmosphereData right) => !Equals(left, right);

        #endregion Equality Members

    }
}