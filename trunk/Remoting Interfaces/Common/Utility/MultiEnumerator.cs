using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Utility {
	public static class MultiEnumerator {
		public static IEnumerable<T> GetEnumerator<T>(params IEnumerable<T>[] enums) {
			foreach (IEnumerable<T> enumerator in enums) {
				foreach (T val in enumerator) {
					yield return val;
				}
			}
		}

		public static IEnumerable<T> GetEnumerator<T>(T v1, params IEnumerable<T>[] enums) {
			yield return v1;
			foreach (IEnumerable<T> enumerator in enums) {
				foreach (T val in enumerator) {
					yield return val;
				}
			}
		}

		public static IEnumerable<T> GetEnumerator<T>(T v1, T v2, params IEnumerable<T>[] enums) {
			yield return v1;
			yield return v2;
			foreach (IEnumerable<T> enumerator in enums) {
				foreach (T val in enumerator) {
					yield return val;
				}
			}
		}

		public static IEnumerable<T> GetEnumerator<T>(T v1, T v2, T v3, params IEnumerable<T>[] enums) {
			yield return v1;
			yield return v2;
			yield return v3;
			foreach (IEnumerable<T> enumerator in enums) {
				foreach (T val in enumerator) {
					yield return val;
				}
			}
		}

		public static IEnumerable<T> GetEnumertor2<T>(params object[] enums) {
			foreach (object obj in enums) {
				if (obj is T) {
					yield return (T)obj;
				}
				else if (obj is IEnumerable<T>) {
					IEnumerable<T> enumerator = (IEnumerable<T>)obj;
					foreach (T val in enumerator) {
					}
				}
			}
		}
	}
}
