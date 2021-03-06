﻿using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace AccessibilityChecker
{
    class ColourChecker
    {
        public List<string> DivToShow = new List<String>();
        public List<string> TextColours = new List<string>();
        public List<string> BackgroundColours = new List<string>();

        public void GetColours(ChromeDriver driver)
        {
            var TextElements = driver.FindElementsByTagName("p");
            foreach(var TextElement in TextElements)
            {

                TextColours.Add(TextElement.GetCssValue("color"));

                if (TextElement.GetCssValue("background").Contains("rgba(0, 0, 0, 0)")){
                    var TextElementParent = TextElement.FindElement(By.XPath("./.."));
                    while (TextElementParent.GetCssValue("background").Contains("rgba(0, 0, 0, 0)"))
                    {
                        TextElementParent = TextElementParent.FindElement(By.XPath("./.."));
                    }
                    BackgroundColours.Add(TextElementParent.GetCssValue("background"));
                    DivToShow.Add(string.Join(" ", TextElement.GetAttribute("innerHTML").ToString().Split().Take(3)));
                }
                else {
                    BackgroundColours.Add(TextElement.GetCssValue("background"));
                    DivToShow.Add(string.Join(" ", TextElement.GetAttribute("innerHTML").ToString().Split().Take(3)));
                }
            }
        }

        public List<string> GetContrastDifference(double r, double g, double b, double rB, double gB, double bB, string div) {
            List<string> results = new List<string>();
            var fontNumber = (299*r + 587*g + 114*b) / 1000;
            var backgroundNumber = (299 * rB + 587 * gB + 114 * bB) / 1000;
            var contrastDifference = 0.00;
            var ratioPercentage = 0.00;
            var ratio = "";

            var gcd = GCD((int)fontNumber, (int)backgroundNumber);

            if (fontNumber >= backgroundNumber)
            {
                contrastDifference = fontNumber - backgroundNumber;
                ratioPercentage = ((fontNumber / gcd) / (backgroundNumber / gcd)) * 100;
                ratio = string.Format("{0}:{1}", fontNumber / gcd, backgroundNumber / gcd);
            }
            else
            {
                contrastDifference = backgroundNumber - fontNumber;
                ratioPercentage = ((backgroundNumber / gcd) / (fontNumber / gcd)) * 100;
                ratio = string.Format("{0}:{1}", backgroundNumber / gcd, fontNumber / gcd);
            }

            if (ratioPercentage < 450)
            {
                Console.WriteLine(string.Format("'{0}' - Contract does not comply with AA (4.5:1), ratio: {1}", div, ratio));
                results.Add(string.Format("'{0}' - Contract does not comply with AA (4.5:1), ratio: {1}", div, ratio));
            } else if(ratioPercentage < 700)
            {
                Console.WriteLine(string.Format("'{0}' - Contrast does not comply with AAA (7:1), ratio: {1}", div, ratio));
                results.Add(string.Format("'{0}' - Contrast does not comply with AAA (7:1), ratio: {1}", div, ratio));
            }

            return results;
        }

        static int GCD(int a, int b)
        {
            return b == 0 ? a : GCD(b, a % b);
        }

        public List<string> GetColourDifference()
        {
            var Passes = 0;
            var Fails = 0;
            List<string> results = new List<string>();
            for (int i = 0; i < TextColours.Count && i < BackgroundColours.Count; i++)
            {
                var colorToConvert = TextColours[i].ToString();
                var splitColour = colorToConvert.Split(',');
                List<string> rgbList = new List<string>();

                foreach (var colour in splitColour)
                {
                    var colourToCheck = "";

                    if (colour.Contains(")"))
                    {
                        colourToCheck = colour.Substring(0, colour.IndexOf(")"));
                        rgbList.Add(string.Join("", colourToCheck.Where(char.IsDigit)));
                    }
                    else
                    {
                        rgbList.Add(string.Join("", colour.Where(char.IsDigit)));
                    }
                }

                var r = Convert.ToDouble(rgbList[0]);
                var g = Convert.ToDouble(rgbList[1]);
                var b = Convert.ToDouble(rgbList[2]);
                if (rgbList.Count == 4)
                {
                    var a = Convert.ToDouble(rgbList[3]);
                }

                var rgbFont = new Rgb { R = r, G = g, B = b };
                var lab = rgbFont.To<Lab>();

                var backToConvert = BackgroundColours[i].ToString();
                var splitBackColour = backToConvert.Split(',');
                List<string> backRgbList = new List<string>();

                foreach (var colour in splitBackColour)
                {
                    var colourToCheck = "";

                    if (colour.Contains(")"))
                    {
                        colourToCheck = colour.Substring(0, colour.IndexOf(")"));
                        backRgbList.Add(string.Join("", colourToCheck.Where(char.IsDigit)));
                    }
                    else
                    {
                        backRgbList.Add(string.Join("", colour.Where(char.IsDigit)));
                    }
                }

                var rB = Convert.ToDouble(backRgbList[0]);
                var gB = Convert.ToDouble(backRgbList[1]);
                var bB = Convert.ToDouble(backRgbList[2]);
                if (backRgbList.Count == 4)
                {
                    var aB = Convert.ToDouble(backRgbList[3]);
                }

                var rgbBack = new Rgb { R = rB, G = gB, B = bB };
                var labBack = rgbBack.To<Lab>();

                double deltaE = lab.Compare(labBack, new Cie1976Comparison());
                if(deltaE < 85.7 && deltaE > 77.8)
                {
                    Fails++;
                    Console.WriteLine("Area w/: " + DivToShow[i] + " | AAA Fail, contrast: " + deltaE.ToString("G5") + "%");
                } else if (deltaE <= 77.8)
                {
                    Console.WriteLine("Area w/: " + DivToShow[i] + " | AA Fail, contrast: " + deltaE.ToString("G5") + "%");
                }
                else
                {
                    Passes++;
                }

                results.AddRange(GetContrastDifference(r, g, b, rB, gB, bB, DivToShow[i]));
            }
            return results;
        }

        // RGB > LAB Conversion
        // Credit goes to https://github.com/THEjoezack/ColorMine for all the below code.
        // http://colormine.org/
        internal static class RgbConverter
        {
            internal static void ToColorSpace(IRgb color, IRgb item)
            {
                item.R = color.R;
                item.G = color.G;
                item.B = color.B;
            }

            internal static IRgb ToColor(IRgb item)
            {
                return item;
            }
        }
        internal static class XyzConverter
        {
            #region Constants/Helper methods for Xyz related spaces
            internal static IXyz WhiteReference { get; private set; } // TODO: Hard-coded!
            internal const double Epsilon = 0.008856; // Intent is 216/24389
            internal const double Kappa = 903.3; // Intent is 24389/27
            static XyzConverter()
            {
                WhiteReference = new Xyz
                {
                    X = 95.047,
                    Y = 100.000,
                    Z = 108.883
                };
            }

            internal static double CubicRoot(double n)
            {
                return Math.Pow(n, 1.0 / 3.0);
            }
            #endregion

            internal static void ToColorSpace(IRgb color, IXyz item)
            {
                var r = PivotRgb(color.R / 255.0);
                var g = PivotRgb(color.G / 255.0);
                var b = PivotRgb(color.B / 255.0);

                // Observer. = 2°, Illuminant = D65
                item.X = r * 0.4124 + g * 0.3576 + b * 0.1805;
                item.Y = r * 0.2126 + g * 0.7152 + b * 0.0722;
                item.Z = r * 0.0193 + g * 0.1192 + b * 0.9505;
            }

            internal static IRgb ToColor(IXyz item)
            {
                // (Observer = 2°, Illuminant = D65)
                var x = item.X / 100.0;
                var y = item.Y / 100.0;
                var z = item.Z / 100.0;

                var r = x * 3.2406 + y * -1.5372 + z * -0.4986;
                var g = x * -0.9689 + y * 1.8758 + z * 0.0415;
                var b = x * 0.0557 + y * -0.2040 + z * 1.0570;

                r = r > 0.0031308 ? 1.055 * Math.Pow(r, 1 / 2.4) - 0.055 : 12.92 * r;
                g = g > 0.0031308 ? 1.055 * Math.Pow(g, 1 / 2.4) - 0.055 : 12.92 * g;
                b = b > 0.0031308 ? 1.055 * Math.Pow(b, 1 / 2.4) - 0.055 : 12.92 * b;

                return new Rgb
                {
                    R = ToRgb(r),
                    G = ToRgb(g),
                    B = ToRgb(b)
                };
            }

            private static double ToRgb(double n)
            {
                var result = 255.0 * n;
                if (result < 0) return 0;
                if (result > 255) return 255;
                return result;
            }

            private static double PivotRgb(double n)
            {
                return (n > 0.04045 ? Math.Pow((n + 0.055) / 1.055, 2.4) : n / 12.92) * 100.0;
            }
        }

        internal static class LabConverter
        {
            internal static void ToColorSpace(IRgb color, ILab item)
            {
                var xyz = new Xyz();
                xyz.Initialize(color);

                var white = XyzConverter.WhiteReference;
                var x = PivotXyz(xyz.X / white.X);
                var y = PivotXyz(xyz.Y / white.Y);
                var z = PivotXyz(xyz.Z / white.Z);

                item.L = Math.Max(0, 116 * y - 16);
                item.A = 500 * (x - y);
                item.B = 200 * (y - z);
            }

            internal static IRgb ToColor(ILab item)
            {
                var y = (item.L + 16.0) / 116.0;
                var x = item.A / 500.0 + y;
                var z = y - item.B / 200.0;

                var white = XyzConverter.WhiteReference;
                var x3 = x * x * x;
                var z3 = z * z * z;
                var xyz = new Xyz
                {
                    X = white.X * (x3 > XyzConverter.Epsilon ? x3 : (x - 16.0 / 116.0) / 7.787),
                    Y = white.Y * (item.L > (XyzConverter.Kappa * XyzConverter.Epsilon) ? Math.Pow(((item.L + 16.0) / 116.0), 3) : item.L / XyzConverter.Kappa),
                    Z = white.Z * (z3 > XyzConverter.Epsilon ? z3 : (z - 16.0 / 116.0) / 7.787)
                };

                return xyz.ToRgb();
            }

            private static double PivotXyz(double n)
            {
                return n > XyzConverter.Epsilon ? CubicRoot(n) : (XyzConverter.Kappa * n + 16) / 116;
            }

            private static double CubicRoot(double n)
            {
                return Math.Pow(n, 1.0 / 3.0);
            }
        }
    }
}
