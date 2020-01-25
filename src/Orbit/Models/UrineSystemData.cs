using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Orbit.Models
{
    public class UrineSystemData : IAlertableModel, IEquatable<UrineSystemData>
    {
        #region Limits

        private const int urineTankUpperLimit = 100;
        private const int urineTankLevelTolerance = 5;

        private const int distillerSpeedUpperLimit = 1300;
        private const int distillerSpeedLowerLimit = 1100;
        private const int distillerSpeedTolerance = 100;

        private const int distillerTempUpperLimit = 55;
        private const int distillerTempLowerLimit = 35;
        private const int distillerTempTolerance = 5;

        private const int brineTankLevelUpperLimit = 100;
        private const int brineTankLevelTolerance = 5;

        private SystemStatus lastWorkingStatus;

        #endregion Limits

        #region Properties

        /// <summary>
        /// The name of the component.
        /// </summary>
        [NotMapped]
        public string ComponentName => "UrineSystem";

        public DateTimeOffset ReportDateTime { get; private set; } = DateTimeOffset.Now;

        /// <summary>
        /// Indicator of overall system status (Ready, Processing, Failure...)
        /// </summary>
        public SystemStatus SystemStatus { get; set; }

        /// <summary>
        /// Fullness of treated urine holding tank as a percentage
        /// </summary>
        [Range(0, urineTankUpperLimit)]
        public double UrineTankLevel { get; set; }

        /// <summary>
        /// status of pump assembly used to pull fluid from urine tank to the distiller assembly then from distiller
        /// assembly to the brine tank and water processor
        /// </summary>
        public bool SupplyPumpOn { get; set; }

        /// <summary>
        /// Turning distiller on or off also turns on/off distiller motor and heater
        /// </summary>
        public bool DistillerOn { get; set; }

        /// <summary>
        /// Motor speed; nominal 1200 rpm
        /// </summary>
        [Range(0, distillerSpeedUpperLimit + distillerSpeedTolerance)]
        public int DistillerSpeed { get; set; }

        /// <summary>
        /// Temp of urine in the distiller; nominal 45C
        /// </summary>
        [Range(0, 60)]
        public double DistillerTemp { get; set; }

        /// <summary>
        /// Routes distillate and gasses from distiller to gas/liquid separator cooled assembly aids condensation of
        /// water from gas
        /// </summary>
        public bool PurgePumpOn { get; set; }

        /// <summary>
        /// Stores concentrated minerals and contaminates from urine distillation process for later disposal shown as percentage
        /// </summary>
        [Range(0, brineTankLevelUpperLimit)]
        public double BrineTankLevel { get; set; }

        #endregion Properties

        #region Constructor

        public UrineSystemData()
        {

        }
        public UrineSystemData (UrineSystemData other)
        {
            ReportDateTime = DateTimeOffset.Now;
            SystemStatus = other.SystemStatus;
            UrineTankLevel = other.UrineTankLevel;
            SupplyPumpOn = other.SupplyPumpOn;
            DistillerOn = other.DistillerOn;
            DistillerTemp = other.DistillerTemp;
            DistillerSpeed = other.DistillerSpeed;
            PurgePumpOn = other.PurgePumpOn;
            BrineTankLevel = other.BrineTankLevel;

        }

        #endregion Constructor

        public void ProcessData(double wasteTankLevel)
        {
            GenerateData();

            if (SystemStatus == SystemStatus.Standby)
            {
                // if urine tank is full and the waste and brine tanks are not, change to 'processing' state
                if (UrineTankLevel >= urineTankUpperLimit * .8
                    && wasteTankLevel < urineTankUpperLimit
                    && BrineTankLevel < brineTankLevelUpperLimit)
                {
                    SystemStatus = SystemStatus.Processing;
                }
                // if urine tank not full, stay in standby and simulate urine tank filling
                else
                {
                    if (SupplyPumpOn || PurgePumpOn || DistillerOn)
                    {
                        lastWorkingStatus = SystemStatus;
                        SystemStatus = SystemStatus.Trouble;
                    }
                    else
                    {
                        SimulateStandby();
                    }
                }
            }
            else if (SystemStatus == SystemStatus.Processing)
            {
                // no more urine to process or waste or brine tank full, change to 'Standby' state
                if (UrineTankLevel <= 0
                    || wasteTankLevel >= urineTankUpperLimit
                    || BrineTankLevel >= brineTankLevelUpperLimit)
                {
                    SystemStatus = SystemStatus.Standby;

                    UrineTankLevel = Math.Max(UrineTankLevel, 0);
                    BrineTankLevel = Math.Min(BrineTankLevel, brineTankLevelUpperLimit);
                }
                else
                {
                    if(!SupplyPumpOn || !PurgePumpOn || !DistillerOn)
                    {
                        lastWorkingStatus = SystemStatus;
                        SystemStatus = SystemStatus.Trouble;
                    }
                    else
                    {
                        SimulateProcessing();
                    }
                }
            }
            else if(SystemStatus == SystemStatus.Trouble)
            {
                Trouble();
            }
            else
            {
                SystemStatus = SystemStatus.Standby;
                SimulateStandby();
            }
        }

        private void SimulateProcessing()
        {
            UrineTankLevel -= 5;
            BrineTankLevel += 1;
            
            // should be on, randomly trigger a false to simulate a fault
            SupplyPumpOn = RandomFalse();
            DistillerOn = RandomFalse();
            PurgePumpOn = RandomFalse();
        }

        private void SimulateStandby()
        {
            UrineTankLevel += 3;

            // should be off, randomly trigger a true to simulate a fault
            SupplyPumpOn = RandomTrue();
            DistillerOn = RandomTrue();
            PurgePumpOn = RandomTrue();
        }

        private void Trouble()
        {
            if(lastWorkingStatus == SystemStatus.Processing)
            {
                SimulateProcessing();
            }
            else
            {
                SimulateStandby();
            }
        }

        private void GenerateData()
        {
            Random rand = new Random();

           if(SystemStatus == SystemStatus.Processing)
            {
                DistillerTemp = rand.Next(distillerTempLowerLimit, distillerTempUpperLimit);
                DistillerSpeed = rand.Next(distillerSpeedLowerLimit, distillerSpeedUpperLimit);
            }
            else
            {
                DistillerTemp = rand.Next(15, 32);  // something close to ambient air temp
                DistillerSpeed = 0;
            }
        }

        private bool RandomTrue()
        {
            Random rand = new Random();

            if (rand.Next(1, 10) == 3)
            {
                return true;
            }
            else { return false; }
        }

        private bool RandomFalse()
        {
            Random rand = new Random();

            if (rand.Next(1, 10) == 3)
            {
                return false;
            }
            else { return true; }
        }

        #region Alert Generation

        private IEnumerable<Alert> CheckUrineTankLevel()
        {
            // system should start/be processing if wastetank < 95%, else shut down
            if (UrineTankLevel >= urineTankUpperLimit)
            {
                yield return new Alert(nameof(UrineTankLevel), "Urine tank level is at capacity", AlertLevel.HighError);
            }
            else if (UrineTankLevel >= (urineTankUpperLimit - urineTankLevelTolerance))
            {
                yield return new Alert(nameof(UrineTankLevel), "Urine tank level is nearing capacity", AlertLevel.HighWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(UrineTankLevel));
            }
        }

        private IEnumerable<Alert> CheckDistillerSpeed()
        {
            if (DistillerSpeed >= distillerSpeedUpperLimit)
            {
                //TODO: shut down system
                yield return new Alert(nameof(DistillerSpeed), "Distiller speed above maximum", AlertLevel.HighError);
            }
            else if (DistillerSpeed > (distillerSpeedUpperLimit - distillerSpeedTolerance))
            {
                yield return new Alert(nameof(DistillerSpeed), "Distiller speed is too high", AlertLevel.HighWarning);
            }
            else if (DistillerOn && (DistillerSpeed < distillerSpeedLowerLimit))
            {
                yield return new Alert(nameof(DistillerSpeed), "Distiller speed is below minimum", AlertLevel.LowError);
            }
            else if (DistillerOn && (DistillerSpeed <= (distillerSpeedLowerLimit + distillerSpeedTolerance)))
            {
                // TODO: shut down system
                yield return new Alert(nameof(DistillerSpeed), "Distiller speed is too low", AlertLevel.LowWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(DistillerSpeed));
            }
        }

        private IEnumerable<Alert> CheckDistillerTemp()
        {
            if (DistillerTemp >= distillerTempUpperLimit)
            {
                yield return new Alert(nameof(DistillerTemp), "Distiller temp above maximum", AlertLevel.HighError);
            }
            else if (DistillerTemp > (distillerTempUpperLimit - distillerTempTolerance))
            {
                yield return new Alert(nameof(DistillerTemp), "Distiller temp is too high", AlertLevel.HighWarning);
            }
            else if (DistillerOn && (DistillerTemp < distillerTempLowerLimit))
            {
                yield return new Alert(nameof(DistillerTemp), "Distiller temp is below minimum", AlertLevel.LowError);
            }
            else if (DistillerOn && (DistillerTemp <= (distillerTempLowerLimit + distillerTempTolerance)))
            {
                yield return new Alert(nameof(DistillerTemp), "Distiller Temp is too low", AlertLevel.LowWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(DistillerTemp));
            }
        }

        private IEnumerable<Alert> CheckBrineTankLevel()
        {
            if (BrineTankLevel >= brineTankLevelUpperLimit)
            {
                // TODO: shut down system
                yield return new Alert(nameof(BrineTankLevel), "Brine tank is at capacity", AlertLevel.HighError);
            }
            else if (BrineTankLevel >= (brineTankLevelUpperLimit - brineTankLevelTolerance))
            {
                yield return new Alert(nameof(BrineTankLevel), "Brine tank is nearing capacity", AlertLevel.HighWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(BrineTankLevel));
            }
        }

        IEnumerable<Alert> IAlertableModel.GenerateAlerts()
        {
            return this.CheckUrineTankLevel()
                .Concat(this.CheckDistillerTemp())
                .Concat(this.CheckDistillerSpeed())
                .Concat(this.CheckBrineTankLevel());
        }

        #endregion Alert Generation

        #region Equality members

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other"> An object to compare with this object. </param>
        /// <returns>
        /// <see langword="true"/> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Equals(UrineSystemData? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return this.ReportDateTime.Equals(other.ReportDateTime)
                   && this.SystemStatus == other.SystemStatus
                   && this.UrineTankLevel.Equals(other.UrineTankLevel)
                   && this.SupplyPumpOn == other.SupplyPumpOn
                   && this.DistillerOn == other.DistillerOn
                   && this.DistillerSpeed == other.DistillerSpeed
                   && this.DistillerTemp.Equals(other.DistillerTemp)
                   && this.PurgePumpOn == other.PurgePumpOn
                   && this.BrineTankLevel.Equals(other.BrineTankLevel);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj"> The object to compare with the current object. </param>
        /// <returns>
        /// <see langword="true"/> if the specified object is equal to the current object; otherwise, <see langword="false"/>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;

            return this.Equals(obj as UrineSystemData);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns> A hash code for the current object. </returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                this.ReportDateTime,
                this.SystemStatus,
                this.UrineTankLevel,
                this.SupplyPumpOn,
                this.DistillerOn,
                this.DistillerSpeed,

                // Have to use tuple bc for some reason the method is capped at 8 args
                (this.DistillerTemp, this.BrineTankLevel)
            );
        }

        /// <summary>
        /// Returns a value that indicates whether the values of two <see cref="T:Orbit.Models.UrineSystemData"/>
        /// objects are equal.
        /// </summary>
        /// <param name="left"> The first value to compare. </param>
        /// <param name="right"> The second value to compare. </param>
        /// <returns>
        /// true if the <paramref name="left"/> and <paramref name="right"/> parameters have the same value; otherwise, false.
        /// </returns>
        public static bool operator ==(UrineSystemData left, UrineSystemData right) => Equals(left, right);

        /// <summary>
        /// Returns a value that indicates whether two <see cref="T:Orbit.Models.UrineSystemData"/> objects have
        /// different values.
        /// </summary>
        /// <param name="left"> The first value to compare. </param>
        /// <param name="right"> The second value to compare. </param>
        /// <returns> true if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, false. </returns>
        public static bool operator !=(UrineSystemData left, UrineSystemData right) => !Equals(left, right);

        #endregion Equality members

    }
}