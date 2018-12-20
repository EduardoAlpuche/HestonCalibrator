using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestHestonCalibrator
{
    class Program
    {
        static void Main(string[] args)
        {
            TestHestonCalibration();
            Console.ReadKey();
        }

        public static void TestHestonCalibration()
        {
            double r0 = 0.1;

            double[] stockPrice = new double[] { 100.0, 100.0, 100.0, 100.0, 100.0 };
            double[] optionExerciseTimes = new double[] { 1.0, 1.0, 2.0, 2.0, 1.5 };
            double[] optionStrikes = new double[] { 80.0, 90.0, 80.0, 100.0, 100.0 };
            double[] prices = new double[] {25.72, 18.93, 30.49, 19.36, 16.58};
            
            HestonCalibrator calibrator = new HestonCalibrator(r0, 1e-3, 1000);
            for (int i = 0; i < prices.Length; ++i)
            {
                calibrator.AddObservedOption(stockPrice[i], optionExerciseTimes[i], optionStrikes[i], prices[i]);
            }

            calibrator.Calibrate();
            double error = 0;
            CalibrationOutcome outcome = CalibrationOutcome.NotStarted;
            calibrator.GetCalibrationStatus(ref outcome, ref error);
            Console.WriteLine("Calibration outcome: {0} and error: {1}", outcome, error);
        }
    }
}
