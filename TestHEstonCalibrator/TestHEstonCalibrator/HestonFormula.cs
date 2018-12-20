using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace TestHestonCalibrator
{
    public class HestonModel
    {
        public const int numModelParams = 5;
        public Complex i = new Complex(0, 1);

        private const int kIndex = 0;
        private const int thetaIndex = 1;
        private const int sigmaIndex = 2;
        private const int rhoIndex = 3;
        private const int vIndex = 4;

        private double r0;
        private double k;
        private double theta;
        private double sigma;
        private double rho;
        private double v;

        public HestonModel() : this(0, 0, 0, 0, 0, 0)
        {
        }

        public HestonModel(HestonModel otherModel) :
            this(otherModel.r0, otherModel.k, otherModel.theta, otherModel.sigma, otherModel.rho, otherModel.v)
        {
        }

        public HestonModel(double r0, double k, double theta, double sigma, double rho, double v)
        {
            this.r0 = r0;
            this.k = k;
            this.theta = theta;
            this.sigma = sigma;
            this.rho = rho;
            this.v = v;
        }

        public HestonModel(double r0, double[] paramsArray)
            : this(r0, paramsArray[kIndex], paramsArray[thetaIndex], paramsArray[sigmaIndex], paramsArray[rhoIndex], paramsArray[vIndex])
        {
        }

        public double GetK() { return k; }
        public double GetTheta() { return theta; }
        public double GetSigma() { return sigma; }
        public double GetRho() { return rho; }
        public double GetV() { return v; }


        public Complex LowerD(double phi, double b, double u)
        {
            return Complex.Sqrt(Complex.Pow(rho * sigma * phi * i - b, 2) - sigma * sigma * (2 * u * phi * i - phi * phi));
        }

        public Complex LowerG(double phi, double b, Complex d)
        {
            return (b - rho * sigma * phi * i - d) / (b - rho * sigma * phi * i + d);
        }

        public Complex UpperC(double tau, double phi, double b, Complex d, Complex g)
        {
            double a = k * theta;
            return r0 * phi * i * tau + a / (sigma * sigma) * ((b - rho * sigma * phi * i - d) * tau - 2 * Complex.Log((1 - g * Complex.Exp(-tau * d)) / (1 - g)));
        }

        public Complex UpperD(double tau, double phi, double b, Complex d, Complex g)
        {
            return (b - rho * sigma * phi * i - d) / (sigma * sigma) * ((1 - Complex.Exp(-tau * d)) / (1 - g * Complex.Exp(-tau * d)));
        }

        public double[] HestonProb(double K, double tau, double S)
        {
            Complex d, g, C, D, Phi;
            double[] P = new double[2];
            double[] b = { k-rho*sigma, k };
            double[] u = { 0.5, -0.5 };
            CompositeIntegrator Integrator = new CompositeIntegrator(4);

            for (int j = 0; j < 2; j++)
            {
                Func<double, double> f = (phi) =>
                {
                    double x = Math.Log(S);
                    d = LowerD(phi, b[j], u[j]);
                    g = LowerG(phi, b[j], d);
                    C = UpperC(tau, phi, b[j], d, g);
                    D = UpperD(tau, phi, b[j], d, g);
                    Phi = Complex.Exp(C + D * v + i * phi * x);
                    Complex c = (Complex.Exp(-i * phi * Math.Log(K)) * Phi) / (i * phi);
                    return c.Real;
                };
                P[j] = 0.5 + (1 / Math.PI) * Integrator.Integrate(f, 0.0001, 50, 1000);
            }
            return P;
        }

        public double[] Price(double K, double T, double S)
        {
            double CallPrice, PutPrice;
            double[] Price = new double[2];
            double[] P = HestonProb(K, T, S);

            CallPrice = Math.Round(S * P[0] - K * Math.Exp(-r0 * T) * P[1], 2);
            //PutPrice = Math.Round(K * Math.Exp(-r * tau) * P[1] - S * P[0], 2);
            PutPrice = Math.Round(K * Math.Exp(-r0 * T) - S + CallPrice, 2);

            Price[0] = CallPrice;
            Price[1] = PutPrice;

            return Price;
        }

        public double CallPrice(double K, double T, double S)
        {
            return Price(K, T, S)[0];
        }

        public double[] ConvertHestonModeCalibrationParamsToArray()
        {
            double[] paramsArray = new double[HestonModel.numModelParams];
            paramsArray[kIndex] = k;
            paramsArray[thetaIndex] = theta;
            paramsArray[sigmaIndex] = sigma;
            paramsArray[rhoIndex] = rho;
            paramsArray[vIndex] = v;
            return paramsArray;
        }
    }
}
