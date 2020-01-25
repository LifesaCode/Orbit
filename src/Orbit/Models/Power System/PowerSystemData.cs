using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Orbit.Models
{
    public enum PowerShuntState
    {
        Charge,
        Discharge
    }

    public class PowerSystemData : IAlertableModel
    {
        #region Private Limits

        private int solarRotationUpperLimit = 205;
        private int solarRotationLowerLimit = -205;
        private int solarRotationTolerance = 5;

        private int solarVoltageUpperLimit = 180;
        private int solarVoltageLowerLimit = 0;
        private int solarVoltageTolerance = 10;

        private int minOutputToCharge = 160;

        private int batteryTemperatureUpperLimit = 35;
        private int batteryTemperatureLowerLimit = -10;
        private int batteryTemperatureTolerance = 10;

        private int batteryChargeLevelUpperLimit = 105;
        private int batteryChargeLevelLowerLimit = 50;
        private int batteryChargeLevelTolerance = 10;

        private int batteryVoltageUpperLimit = 160;
        private int batteryVoltageLowerLimit = 110;
        private int batteryVoltageTolerance = 10;

        private int eclipseCount;
        private int eclipseLength = 20;
        private bool inEclipse;

        #endregion Private Limits

        #region Public Properties

        [NotMapped]
        public string ComponentName => "Power System";
        public DateTimeOffset ReportDateTime { get; private set; } = DateTimeOffset.Now;

        /// <summary>
        /// the overall status of the system 
        /// </summary>
        public SystemStatus Status { get; set; }

        /// <summary>
        /// determines whether the batteries are charging or discharging
        /// </summary>
        public PowerShuntState ShuntStatus { get; set; }

        /// <summary>
        /// the positional rotation of the solar array in degrees
        /// </summary>
        [Range(-205, 205)]
        public int SolarArrayRotation { get; set; }

        /// <summary>
        /// direction of panel movement as degrees are increasing or decreasing 
        /// </summary>
        public bool SolarRotationIncreasing { get; set; }

        /// <summary>
        /// voltage output from solar array, will be primary power for station when >= 160v
        /// </summary>
        [Range(0, 180)]
        public int SolarArrayVoltage { get; set; }

        /// <summary>
        /// true when solar array is extended, false when array is retracted
        /// </summary>
        public bool SolarDeployed { get; set; }

        /// <summary>
        /// temperature of the external battery pack
        /// </summary>
        [Range(-115, 130)]
        public double BatteryTemperature { get; set; }

        /// <summary>
        /// charge level as a percentage 0 - 100
        /// </summary>
        [Range(0, 104)]
        public double BatteryChargeLevel { get; set; }

        /// <summary>
        /// current voltage output from batteries
        /// </summary>
        [Range(0, 170)]
        public double BatteryVoltage { get; set; }

        #endregion Public Properties

        #region Constructors

        public PowerSystemData() { }

        public PowerSystemData(PowerSystemData other)
        {
            ReportDateTime = DateTimeOffset.Now;
            Status = other.Status;
            ShuntStatus = other.ShuntStatus;
            SolarArrayRotation = other.SolarArrayRotation;
            SolarRotationIncreasing = other.SolarRotationIncreasing;
            SolarArrayVoltage = other.SolarArrayVoltage;
            SolarDeployed = other.SolarDeployed;
            BatteryVoltage = other.BatteryVoltage;
            BatteryChargeLevel = other.BatteryChargeLevel;

            // for creating rudimentory sun/eclipse cycles
            inEclipse = other.inEclipse;
            eclipseCount = other.eclipseCount;
        }

        #endregion Constructors


        #region Public Methods

        public void ProcessData()
        {
            GenerateData();

            if ((BatteryTemperature >= batteryTemperatureUpperLimit)
                || (BatteryTemperature <= batteryTemperatureLowerLimit))
            {
                Trouble();
            }

            if (SolarArrayVoltage < minOutputToCharge)
            {
                ShuntStatus = PowerShuntState.Discharge;
                SimulateDrain();
            }
            else
            {
                ShuntStatus = PowerShuntState.Charge;
                SimulateCharge();
            }

            RotatePanels();
        }

        #endregion Public Methods


        #region Private Methods

        private void GenerateData()
        {
            Random rand = new Random();

            // toggle between 'day' and 'night' cycles when solar panels will and will not be generating power
            if (eclipseCount >= eclipseLength)
            {
                inEclipse = !inEclipse;
                eclipseCount = 0;
            }
            else
            {
                eclipseCount++;
            }

            // no voltage can be generated if the solar panels are retracted
            if (!SolarDeployed)
            {
                SolarArrayVoltage = 0;
            }
            // simulatse station behind Earth or Moon, so no sunlight on solar panels
            else if (inEclipse)
            {
                SolarArrayVoltage = rand.Next(minOutputToCharge, solarVoltageUpperLimit);
            }
            // simulates station in sun, so panels can produce voltage
            else
            {
                SolarArrayVoltage = rand.Next(solarVoltageLowerLimit, solarVoltageLowerLimit + solarVoltageTolerance);
            }

            // get a new battery temperature
            BatteryTemperature = rand.Next(batteryTemperatureLowerLimit, batteryTemperatureUpperLimit);

        }
        private void SimulateDrain()
        {
            if (BatteryChargeLevel <= batteryChargeLevelLowerLimit
                || (BatteryVoltage < batteryVoltageLowerLimit)
                || (BatteryVoltage > batteryVoltageUpperLimit))
            {
                Trouble();
            }

            if (BatteryChargeLevel > 0)
            {
                BatteryChargeLevel--;
            }
            else
            {
                BatteryChargeLevel = 0;
            }
        }

        private void SimulateCharge()
        {
            if ((SolarArrayVoltage > solarVoltageUpperLimit)
                || (SolarArrayVoltage < minOutputToCharge))
            {
                Trouble();
            }

            if (BatteryChargeLevel < batteryChargeLevelUpperLimit)
            {
                BatteryChargeLevel++;
            }
            else
            {
                BatteryChargeLevel = batteryChargeLevelUpperLimit;
            }
        }

        private void RotatePanels()
        {
            // rotate solar panel back and forth between range bounds
            if (SolarRotationIncreasing && (SolarArrayRotation < solarRotationUpperLimit))
            {
                SolarArrayRotation++;
            }
            else if (!SolarRotationIncreasing && (SolarArrayRotation > solarRotationLowerLimit))
            {
                SolarArrayRotation--;
            }
            else
            {
                // reached a bound, switch direction
                SolarRotationIncreasing = !SolarRotationIncreasing;
            }
        }

        private void Trouble()
        {
            Status = SystemStatus.Trouble;
        }

        #endregion Private Methods

        #region Check Alerts

        private IEnumerable<Alert> CheckSolarVoltage()
        {
            if (SolarArrayVoltage > solarVoltageUpperLimit)
            {
                yield return new Alert(nameof(SolarArrayVoltage), "Voltage is above limit", AlertLevel.HighError);
            }
            else if (SolarArrayVoltage >= (solarVoltageUpperLimit - solarVoltageTolerance))
            {
                yield return new Alert(nameof(SolarArrayVoltage), "Voltage output is elevated", AlertLevel.HighWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(SolarArrayVoltage));
            }
        }

        private IEnumerable<Alert> CheckSolarRotation()
        {
            if (SolarArrayRotation > solarRotationUpperLimit)
            {
                yield return new Alert(nameof(SolarArrayRotation), "Solar array has exceeded maximum rotation", AlertLevel.HighError);
            }
            else if (SolarArrayRotation >= (solarRotationUpperLimit - solarRotationTolerance))
            {
                yield return new Alert(nameof(SolarArrayRotation), "Solar array rotation is at maximum", AlertLevel.HighWarning);
            }
            else if (SolarArrayRotation < solarRotationLowerLimit)
            {
                yield return new Alert(nameof(SolarArrayRotation), "Solar array has exceeded maximum rotation", AlertLevel.LowError);
            }
            else if (SolarArrayRotation <= (solarRotationLowerLimit - solarRotationTolerance))
            {
                yield return new Alert(nameof(SolarArrayRotation), "Solar array rotation is at maximum", AlertLevel.LowWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(SolarArrayRotation));
            }
        }

        private IEnumerable<Alert> CheckBatteryChargeLevel()
        {
            if (BatteryChargeLevel > batteryChargeLevelUpperLimit)
            {
                yield return new Alert(nameof(BatteryChargeLevel), "Battery charge has exceeded maximum", AlertLevel.HighError);
            }
            else if (BatteryChargeLevel <= (batteryChargeLevelLowerLimit + batteryChargeLevelTolerance))
            {
                yield return new Alert(nameof(BatteryChargeLevel), "Battery charge is approaching minimum", AlertLevel.LowWarning);
            }
            else if (BatteryChargeLevel < batteryChargeLevelLowerLimit)
            {
                yield return new Alert(nameof(BatteryChargeLevel), "Battery charge level is below minimum", AlertLevel.LowError);
            }
            else
            {
                yield return Alert.Safe(nameof(BatteryChargeLevel));
            }
        }

        private IEnumerable<Alert> CheckBatteryVoltage()
        {
            if (BatteryVoltage > batteryVoltageUpperLimit)
            {
                yield return new Alert(nameof(BatteryVoltage), "Battery voltage has exceeded maximum", AlertLevel.HighError);
            }
            else if (BatteryVoltage >= (batteryVoltageUpperLimit - batteryVoltageTolerance))
            {
                yield return new Alert(nameof(BatteryVoltage), "Battery voltage is high", AlertLevel.HighWarning);
            }
            else if (BatteryVoltage < batteryVoltageLowerLimit)
            {
                yield return new Alert(nameof(BatteryVoltage), "Battery voltage is below minimum", AlertLevel.LowError);
            }
            else if (BatteryVoltage <= (batteryVoltageLowerLimit - batteryVoltageTolerance))
            {
                yield return new Alert(nameof(BatteryVoltage), "Battery voltage is low", AlertLevel.LowWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(BatteryVoltage));
            }
        }

        private IEnumerable<Alert> CheckBatteryTemp()
        {
            if (BatteryTemperature > batteryVoltageUpperLimit)
            {
                yield return new Alert(nameof(BatteryTemperature), "Battery temperature is above maximum", AlertLevel.HighError);
            }
            else if (BatteryTemperature >= (batteryTemperatureUpperLimit - batteryTemperatureTolerance))
            {
                yield return new Alert(nameof(BatteryTemperature), "Battery temperature is high", AlertLevel.HighWarning);
            }
            else if (BatteryTemperature < batteryTemperatureLowerLimit)
            {
                yield return new Alert(nameof(BatteryTemperature), "Battery temperature is below minumum", AlertLevel.LowError);
            }
            else if (BatteryTemperature <= (batteryTemperatureLowerLimit - batteryTemperatureTolerance))
            {
                yield return new Alert(nameof(BatteryTemperature), "Battery temperature is low", AlertLevel.LowWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(BatteryTemperature));
            }
        }

        IEnumerable<Alert> IAlertableModel.GenerateAlerts()
        {
            return this.CheckBatteryChargeLevel()
                .Concat(CheckBatteryTemp())
                .Concat(CheckBatteryVoltage())
                .Concat(CheckSolarRotation())
                .Concat(CheckSolarVoltage());
        }

        #endregion CheckAlerts

        #region Equality Members

        public bool Equals(PowerSystemData other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return this.ReportDateTime.Equals(other.ReportDateTime)
                && this.Status == other.Status
                && this.ShuntStatus == other.ShuntStatus
                && this.SolarArrayRotation == other.SolarArrayRotation
                && this.SolarRotationIncreasing == other.SolarRotationIncreasing
                && this.SolarArrayVoltage == other.SolarArrayVoltage
                && this.SolarDeployed == other.SolarDeployed
                && this.BatteryTemperature == other.BatteryTemperature
                && this.BatteryChargeLevel == other.BatteryChargeLevel
                && this.BatteryVoltage == other.BatteryVoltage;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is PowerSystemData other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                this.ReportDateTime,
                this.Status,
                this.ShuntStatus,
                this.SolarArrayRotation,
                this.SolarRotationIncreasing,
                this.SolarArrayVoltage,
                this.SolarDeployed,

                (this.BatteryTemperature, this.BatteryChargeLevel, this.BatteryVoltage)
                );
        }

        public static bool operator ==(PowerSystemData left, PowerSystemData right) => Equals(left, right);

        public static bool operator !=(PowerSystemData left, PowerSystemData right) => !Equals(left, right);

        #endregion Equality Members
    }
}
