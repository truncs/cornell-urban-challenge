using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace UrbanChallenge.OperationalUI.Utilities {
	static class ImageColorizer {
		public static void Colorize(Bitmap image, Color targetColor) {
			HLSRGB target = new HLSRGB(targetColor);

			// iterate through each pixel in the image and set it's hue to match what we want
			for (int j = 0; j < image.Height; j++) {
				for (int i = 0; i < image.Width; i++) {
					Color originalColor = image.GetPixel(i, j);
					HLSRGB pixel = new HLSRGB(originalColor);
					pixel.Hue = target.Hue;
					pixel.Saturation = target.Saturation;
					image.SetPixel(i, j, Color.FromArgb(originalColor.A, pixel.Color));
				}
			}
		}

		/// <summary>
		/// Summary description for HLSRGB.
		/// </summary>
		private struct HLSRGB {
			private byte red;
			private byte green;
			private byte blue;

			private float hue;
			private float luminance;
			private float saturation;

			public byte Red {
				get {
					return red;
				}
				set {
					red = value;
					ToHLS();
				}
			}
			public byte Green {
				get {
					return green;
				}
				set {
					green = value;
					ToHLS();
				}
			}
			public byte Blue {
				get {
					return blue;
				}
				set {
					blue = value;
					ToHLS();
				}
			}
			public float Luminance {
				get {
					return luminance;
				}
				set {
					if ((luminance < 0.0f) || (luminance > 1.0f)) {
						throw new ArgumentOutOfRangeException("Luminance", "Luminance must be between 0.0 and 1.0");
					}
					luminance = value;
					ToRGB();
				}
			}
			public float Hue {
				get {
					return hue;
				}
				set {
					if ((hue < 0.0f) || (hue > 360.0f)) {
						throw new ArgumentOutOfRangeException("Hue", "Hue must be between 0.0 and 360.0");
					}
					hue = value;
					ToRGB();
				}
			}
			public float Saturation {
				get {
					return saturation;
				}
				set {
					if ((saturation < 0.0f) || (saturation > 1.0f)) {
						throw new ArgumentOutOfRangeException("Saturation", "Saturation must be between 0.0 and 1.0");
					}
					saturation = value;
					ToRGB();
				}
			}

			public Color Color {
				get {
					Color c = Color.FromArgb(red, green, blue);
					return c;
				}
				set {
					red = value.R;
					green = value.G;
					blue = value.B;
					ToHLS();
				}
			}

			public void LightenColor(float lightenBy) {
				luminance *= (1.0f + lightenBy);
				if (luminance > 1.0f) {
					luminance = 1.0f;
				}
				ToRGB();
			}

			public void DarkenColor(float darkenBy) {
				luminance *= darkenBy;
				ToRGB();
			}


			public HLSRGB(Color c) {
				red = c.R;
				green = c.G;
				blue = c.B;
				hue = 0;
				luminance = 0;
				saturation = 0;
				ToHLS();
			}

			public HLSRGB(float hue, float luminance, float saturation) {
				this.hue = hue;
				this.luminance = luminance;
				this.saturation = saturation;
				red = 0;
				green = 0;
				blue = 0;
				ToRGB();
			}

			public HLSRGB(byte red, byte green, byte blue) {
				this.red = red;
				this.green = green;
				this.blue = blue;
				hue = 0;
				luminance = 0;
				saturation = 0;
				ToHLS();
			}

			public HLSRGB(HLSRGB hlsrgb) {
				this.red = hlsrgb.Red;
				this.blue = hlsrgb.Blue;
				this.green = hlsrgb.Green;
				this.luminance = hlsrgb.Luminance;
				this.hue = hlsrgb.Hue;
				this.saturation = hlsrgb.Saturation;
			}

			private void ToHLS() {
				byte minval = Math.Min(red, Math.Min(green, blue));
				byte maxval = Math.Max(red, Math.Max(green, blue));

				float mdiff  = (float)(maxval - minval);
				float msum   = (float)(maxval + minval);

				luminance = msum / 510.0f;

				if (maxval == minval) {
					saturation = 0.0f;
					hue = 0.0f;
				}
				else {
					float rnorm = (maxval - red) / mdiff;
					float gnorm = (maxval - green) / mdiff;
					float bnorm = (maxval - blue) / mdiff;

					saturation = (luminance <= 0.5f) ? (mdiff / msum) : (mdiff / (510.0f - msum));

					if (red == maxval) {
						hue = 60.0f * (6.0f + bnorm - gnorm);
					}
					if (green == maxval) {
						hue = 60.0f * (2.0f + rnorm - bnorm);
					}
					if (blue  == maxval) {
						hue = 60.0f * (4.0f + gnorm - rnorm);
					}
					if (hue > 360.0f) {
						hue = hue - 360.0f;
					}
				}
			}

			private void ToRGB() {
				// Grauton, einfacher Fall
				if (saturation == 0.0) {
					red = (byte)(luminance * 255.0F);
					green = red;
					blue = red;
				}
				else {
					float rm1;
					float rm2;

					if (luminance <= 0.5f) {
						rm2 = luminance + luminance * saturation;
					}
					else {
						rm2 = luminance + saturation - luminance * saturation;
					}
					rm1 = 2.0f * luminance - rm2;
					red   = ToRGB1(rm1, rm2, hue + 120.0f);
					green = ToRGB1(rm1, rm2, hue);
					blue  = ToRGB1(rm1, rm2, hue - 120.0f);
				}
			}

			private byte ToRGB1(float rm1, float rm2, float rh) {
				if (rh > 360.0f) {
					rh -= 360.0f;
				}
				else if (rh <   0.0f) {
					rh += 360.0f;
				}

				if (rh <  60.0f) {
					rm1 = rm1 + (rm2 - rm1) * rh / 60.0f;
				}
				else if (rh < 180.0f) {
					rm1 = rm2;
				}
				else if (rh < 240.0f) {
					rm1 = rm1 + (rm2 - rm1) * (240.0f - rh) / 60.0f;
				}

				return (byte)(rm1 * 255);
			}
		}
	}
}
