using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestHestonCalibrator
{
    public class CalibrationFailedException : Exception
    {
        public CalibrationFailedException()
        {
        }
        public CalibrationFailedException(string message)
            : base(message)
        {
        }
    }

    public enum CalibrationOutcome
    {
        NotStarted,
        FinishedOK,
        FailedMaxItReached,
        FailedOtherReason
    };

    public struct EuropeanCallOptionMarketData
    {
        public double stockPrice; //S
        public double optionExercise; //T
        public double strike; //K
        public double marketMidPrice; //Price
    }

    public class HestonCalibrator
    {
        private const double defaultAccuracy = 10e-3;
        private const int defaultMaxIterations = 500;
        private double accuracy;
        private int maxIterations;

        private LinkedList<EuropeanCallOptionMarketData> marketOptionsList;
        private double r0; // initial interest rate, this is observed, no need to calibrate to options

        private CalibrationOutcome outcome;

        private double[] calibratedParams;

        public HestonCalibrator()
        {
            accuracy = defaultAccuracy;
            maxIterations = defaultMaxIterations;
            marketOptionsList = new LinkedList<EuropeanCallOptionMarketData>();
            r0 = 0.1;
            calibratedParams = new double[] { 2, 0.06, 0.4, 0.5, 0.04 };
        }

        public HestonCalibrator(double r0, double accuracy, int maxIterations)
        {
            this.r0 = r0;
            this.accuracy = accuracy;
            this.maxIterations = maxIterations;
            marketOptionsList = new LinkedList<EuropeanCallOptionMarketData>();
            calibratedParams = new double[] { 2, 0.06, 0.4, 0.5, 0.04 };
        }

        public void SetGuessParameters(double k, double theta, double sigma, double rho, double v)
        {
            HestonModel m = new HestonModel(r0, k, theta, sigma, rho, v);
            calibratedParams = m.ConvertHestonModeCalibrationParamsToArray();
        }

        public void AddObservedOption(double stockPrice, double optionExercise, double strike, double mktMidPrice)
        {
            EuropeanCallOptionMarketData observedOption;
            observedOption.stockPrice = stockPrice;
            observedOption.optionExercise = optionExercise;
            observedOption.strike = strike;
            observedOption.marketMidPrice = mktMidPrice;
            marketOptionsList.AddLast(observedOption);
        }

        // Calculate difference between observed and model prices
        public double CalcMeanSquareErrorBetweenModelAndMarket(HestonModel m)
        {
            double meanSqErr = 0;
            foreach (EuropeanCallOptionMarketData option in marketOptionsList)
            {
                double stockPrice = option.stockPrice;
                double optionExercise = option.optionExercise;
                double strike = option.strike;
                double modelPrice = m.CallPrice(strike, optionExercise, stockPrice);

                //Console.WriteLine("Model = {0}   Market = {1}", modelPrice, option.marketMidPrice);
                double difference = modelPrice - option.marketMidPrice;
                meanSqErr += difference * difference;
            }
            return meanSqErr;
        }

        // Used by Alglib minimisation algorithm
        public void CalibrationObjectiveFunction(double[] paramsArray, ref double func, object obj)
        {
            HestonModel m = new HestonModel(r0, paramsArray);
            //Console.WriteLine("{0} {1} {2} {3} {4}", paramsArray[0], paramsArray[1], paramsArray[2], paramsArray[3], paramsArray[4]);
            func = CalcMeanSquareErrorBetweenModelAndMarket(m);
        }

        public void Calibrate()
        {
            outcome = CalibrationOutcome.NotStarted;

            double[] initialParams = new double[HestonModel.numModelParams];
            calibratedParams.CopyTo(initialParams, 0);  // a reasonable starting guees
            
            double epsg = accuracy;
            double epsf = accuracy; //1e-4;
            double epsx = accuracy;
            double diffstep = 1.0e-6;
            int maxits = maxIterations;
            double stpmax = 0.05;

            alglib.minlbfgsstate state;
            alglib.minlbfgsreport rep;
            alglib.minlbfgscreatef(1, initialParams, diffstep, out state);
            alglib.minlbfgssetcond(state, epsg, epsf, epsx, maxits);
            alglib.minlbfgssetstpmax(state, stpmax);

            // this will do the work
            alglib.minlbfgsoptimize(state, CalibrationObjectiveFunction, null, null);
            double[] resultParams = new double[HestonModel.numModelParams];
            alglib.minlbfgsresults(state, out resultParams, out rep);
           
            System.Console.WriteLine("Termination type: {0}", rep.terminationtype);
            System.Console.WriteLine("Num iterations {0}", rep.iterationscount);
            System.Console.WriteLine("{0}", alglib.ap.format(resultParams, 5));

            if (rep.terminationtype == 1			// relative function improvement is no more than EpsF.
                || rep.terminationtype == 2			// relative step is no more than EpsX.
                || rep.terminationtype == 4)
            {    	// gradient norm is no more than EpsG
                outcome = CalibrationOutcome.FinishedOK;
                // we update the ''inital parameters''
                calibratedParams = resultParams;
            }
            else if (rep.terminationtype == 5)
            {	// MaxIts steps was taken
                outcome = CalibrationOutcome.FailedMaxItReached;
                // we update the ''inital parameters'' even in this case
                calibratedParams = resultParams;

            }
            else
            {
                outcome = CalibrationOutcome.FailedOtherReason;
                throw new CalibrationFailedException("Heston model calibration failed badly.");
            }
        }

        public void GetCalibrationStatus(ref CalibrationOutcome calibOutcome, ref double pricingError)
        {
            calibOutcome = outcome;
            HestonModel m = new HestonModel(r0, calibratedParams);
            pricingError = CalcMeanSquareErrorBetweenModelAndMarket(m);
        }

        public HestonModel GetCalibratedModel()
        {
            HestonModel m = new HestonModel(r0, calibratedParams);
            return m;
        }
    }
}
