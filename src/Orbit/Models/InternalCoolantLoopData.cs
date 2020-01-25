using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Orbit.Models
{
    public class InternalCoolantLoopData : IAlertableModel
    {
        #region Limits

        private int mixValveMaxOpen = 100;
        private int mixValveMaxClosed = 0;
        private int mixValveTolerance = 5;

        private const double medCoolantLoopUpperLimit = 22;
        private const double medCoolantLoopLowerLimit = 12;
        private const double medCoolantLoopTolerance = 5;

        private const double lowTempCoolantLoopUpperLimit = 10;
        private const double lowTempCoolantLoopLowerLimit = 2;
        private const double lowTempCoolantLoopTolerance = 2;

        #endregion Limits

        #region Public Properties

        [NotMapped]
        public string ComponentName => "InternalCoolantSystem";

        public DateTimeOffset ReportDateTime { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// overall system status
        /// </summary>
        public SystemStatus Status { get; set; }

        /// <summary>
        /// true if pump is working, false if not working
        /// </summary>
        public bool LowTempPumpOn { get; set; }

        /// <summary>
        /// true if pump is working, false if not working
        /// </summary>
        public bool MedTempPumpOn { get; set; }

        /// <summary>
        /// determines mix of 'hot' coolant returning from equipment and 'cold' coolant coming from heat exchanger
        /// 0 = bypass heat exhanger, 100 = no fluid bypasses heat exchanger
        /// </summary>
        public int HeatExMixValvePosition { get; set; }

        /// <summary>
        /// determines mix of 'med' and 'low' temp coolants, allows secondary control of temp and 
        /// operation of seperate loops as one loop in case of a pump failure
        /// 0 = operate as seperate loops, 100 = operate as single loop
        /// </summary>
        public int LoopMixValvePosition { get; set; }

        /// <summary>
        /// warmer temperature coolant loop for experiments and avionics 
        /// nominal is 10.5C
        /// </summary>
        [Range(0, 47)]
        public double TempMedCoolantLoop { get; set; }

        /// <summary>
        /// colder temperature coolant loop for life support, cabin air assembly, and some experiments 
        /// nominal is 4C
        /// </summary>
        [Range(0, 47)]
        public double TempLowCoolantLoop { get; set; }

        /// <summary>
        /// desired temperature for the medium temperature loop
        /// </summary>
        public double SetTempMedLoop { get; set; }

        /// <summary>
        /// desired temperature for the low temperature loop
        /// </summary>
        public double SetTempLowLoop { get; set; }

        #endregion Public Properties

        #region Methods

        public void ProcessData()
        {
            GenerateData();

            if(TempLowCoolantLoop < SetTempLowLoop)
            {
                // coolant too cool, decrease amount of 'cold' coolant from heat exchanger
                DecreaseMix();   
            }

            if(TempLowCoolantLoop > SetTempLowLoop)
            {
                // coolant too warm, add more 'cold' coolant from heat exchanger
                IncreaseMix();
            }

            if((!LowTempPumpOn || !MedTempPumpOn) && LoopMixValvePosition != mixValveMaxOpen)
            {
                // if a pump goes off, make sure loops are operating as single loop to maintain cooling ability
                LoopMixValvePosition = mixValveMaxOpen;
                Status = SystemStatus.Trouble;
            }

            if ((TempLowCoolantLoop <= lowTempCoolantLoopLowerLimit) && (HeatExMixValvePosition != mixValveMaxClosed))
            {
                // temp is too low, bypass heat exchanger so line does not freeze
                HeatExMixValvePosition = mixValveMaxClosed;
            }
        }

        private void GenerateData()
        {
            Random rand = new Random();

            TempLowCoolantLoop = rand.Next(0, 60);
            TempMedCoolantLoop = rand.Next(0, 60);

            if (rand.Next(0, 10) == 5)
            {
                LowTempPumpOn = !LowTempPumpOn;
            }
            if (rand.Next(0, 10) == 9)
            {
                MedTempPumpOn = !MedTempPumpOn;
            }
        }

        private void IncreaseMix()
        {
            if(HeatExMixValvePosition < mixValveMaxOpen)
            {
                HeatExMixValvePosition++;
            }
            else
            {
                HeatExMixValvePosition = mixValveMaxOpen;
            }
        }

        private void DecreaseMix()
        {
            if (HeatExMixValvePosition > mixValveMaxClosed)
            {
                HeatExMixValvePosition--;
            }
            else 
            {
                HeatExMixValvePosition = 0;
            }
        }

        #endregion Methods

        #region Alert Generation

        private IEnumerable<Alert> CheckLowLoopTemp()
        {
            if (TempLowCoolantLoop >= lowTempCoolantLoopUpperLimit)
            {
                yield return new Alert(nameof(TempLowCoolantLoop), "Low coolant loop temperature is above maximum", AlertLevel.HighError);
            }
            else if (TempLowCoolantLoop >= (lowTempCoolantLoopUpperLimit - lowTempCoolantLoopTolerance))
            {
                yield return new Alert(nameof(TempLowCoolantLoop), "Low coolant loop temperature is high", AlertLevel.HighWarning);
            }
            else if (TempLowCoolantLoop <= lowTempCoolantLoopLowerLimit)
            {
                yield return new Alert(nameof(TempLowCoolantLoop), "Low coolant loop temperature is low", AlertLevel.LowError);
            }
            else if (TempLowCoolantLoop <= (lowTempCoolantLoopLowerLimit + lowTempCoolantLoopTolerance))
            {
                yield return new Alert(nameof(TempLowCoolantLoop), "Low coolant loop temperature is below minimum", AlertLevel.LowWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(TempLowCoolantLoop));
            }
        }

        private IEnumerable<Alert> CheckMedLoopTemp()
        {
            if (TempMedCoolantLoop >= medCoolantLoopUpperLimit)
            {
                yield return new Alert(nameof(TempMedCoolantLoop), "Med coolant loop temperature is above maximum", AlertLevel.HighError);
            }
            else if (TempMedCoolantLoop >= (medCoolantLoopUpperLimit - medCoolantLoopTolerance))
            {
                yield return new Alert(nameof(TempMedCoolantLoop), "Med coolant loop temperature is high", AlertLevel.HighWarning);
            }
            else if (TempMedCoolantLoop <= medCoolantLoopLowerLimit)
            {
                yield return new Alert(nameof(TempMedCoolantLoop), "Med coolant loop temperature is low", AlertLevel.LowError);
            }
            else if (TempMedCoolantLoop <= (medCoolantLoopLowerLimit + medCoolantLoopTolerance))
            {
                yield return new Alert(nameof(TempMedCoolantLoop), "Med coolant loop temperature is below minimum", AlertLevel.LowWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(TempMedCoolantLoop));
            }
        }

        private IEnumerable<Alert> CheckHeatExMixValve()
        {
            if(HeatExMixValvePosition >= mixValveMaxOpen)
            {
                yield return new Alert(nameof(HeatExMixValvePosition), "Internal coolant heat exchange mixing valve is fully open", AlertLevel.HighError);
            }
            if(HeatExMixValvePosition > (mixValveMaxOpen - mixValveTolerance))
            {
                yield return new Alert(nameof(HeatExMixValvePosition), "Internal collant heat exchange mixing valve is almost fully open", AlertLevel.HighWarning);
            }
            if(HeatExMixValvePosition <= mixValveMaxClosed)
            {
                yield return new Alert(nameof(HeatExMixValvePosition), "Internal coolant heat exchange mixing valve is fully closed", AlertLevel.LowError);
            }
            if(HeatExMixValvePosition < (mixValveMaxClosed - mixValveTolerance))
            {
                yield return new Alert(nameof(HeatExMixValvePosition), "Internal coolant heat exchange mixing valve is almost fully closed", AlertLevel.LowWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(HeatExMixValvePosition));
            }
        }

        private IEnumerable<Alert> CheckLoopMixValve()
        {
            if (LoopMixValvePosition >= mixValveMaxOpen)
            {
                yield return new Alert(nameof(LoopMixValvePosition), "Internal coolant loop mixing valve is fully open", AlertLevel.HighError);
            }
            if (LoopMixValvePosition > (mixValveMaxOpen - mixValveTolerance))
            {
                yield return new Alert(nameof(LoopMixValvePosition), "Internal coolant loop mixing valve is almost fully open", AlertLevel.HighWarning);
            }
            if (LoopMixValvePosition <= mixValveMaxClosed)
            {
                yield return new Alert(nameof(LoopMixValvePosition), "Internal coolant loop mixing valve is fully closed", AlertLevel.LowError);
            }
            if (LoopMixValvePosition < (mixValveMaxClosed - mixValveTolerance))
            {
                yield return new Alert(nameof(LoopMixValvePosition), "Internal coolant loop mixing valve is almost fully closed", AlertLevel.LowWarning);
            }
            else
            {
                yield return Alert.Safe(nameof(LoopMixValvePosition));
            }
        }

        private IEnumerable<Alert> CheckLowTempPump()
        {
            if (!LowTempPumpOn)
            {
                yield return new Alert(nameof(LowTempPumpOn), "Low temperature pump is off", AlertLevel.HighError);
            }
            else
            {
                yield return Alert.Safe(nameof(LowTempPumpOn));
            }
        }

        private IEnumerable<Alert> CheckMedTempPump()
        {
            if (!MedTempPumpOn)
            {
                yield return new Alert(nameof(MedTempPumpOn), "Med temperature pump is off", AlertLevel.HighError);
            }
            else
            {
                yield return Alert.Safe(nameof(MedTempPumpOn));
            }
        }


        IEnumerable<Alert> IAlertableModel.GenerateAlerts()
        {
            return this.CheckLowLoopTemp()
                .Concat(CheckMedLoopTemp())
                .Concat(CheckHeatExMixValve())
                .Concat(CheckLoopMixValve())
                .Concat(CheckLowTempPump())
                .Concat(CheckMedTempPump());
        }

        #endregion Alert Generation

        #region Equality Members

        public bool Equals(InternalCoolantLoopData other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if(ReferenceEquals(this, other))
                return true;
            return this.ReportDateTime == other.ReportDateTime
                && this.Status == other.Status
                && this.LowTempPumpOn == other.LowTempPumpOn
                && this.MedTempPumpOn == other.MedTempPumpOn
                && this.HeatExMixValvePosition == other.HeatExMixValvePosition
                && this.LoopMixValvePosition == other.LoopMixValvePosition
                && this.TempMedCoolantLoop == other.TempMedCoolantLoop
                && this.TempLowCoolantLoop == other.TempLowCoolantLoop
                && this.SetTempMedLoop == other.SetTempMedLoop
                && this.SetTempLowLoop == other.SetTempLowLoop;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is InternalCoolantLoopData other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                this.ReportDateTime,
                this.Status,
                this.LowTempPumpOn,
                this.MedTempPumpOn,
                this.HeatExMixValvePosition,
                this.LoopMixValvePosition,
                this.TempLowCoolantLoop,
                (this.TempMedCoolantLoop, this.SetTempLowLoop, this.SetTempMedLoop)
            );
        }

        public static bool operator ==(InternalCoolantLoopData left, InternalCoolantLoopData right) => Equals(left, right);

        public static bool operator !=(InternalCoolantLoopData left, InternalCoolantLoopData right) => !Equals(left, right);

        #endregion Equality Members
    }
}