using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Dataset.Utility {
	internal class WriterLock : IDisposable {
		private ReaderWriterLock rwlock;
		private bool acquiredWriteLock;
		private LockCookie lc;
		private bool upgradedFromReadLock;

		internal WriterLock(LockCookie lc, ReaderWriterLock rwlock) {
			upgradedFromReadLock = true;
			this.lc = lc;
			this.rwlock = rwlock;
		}

		public WriterLock(ReaderWriterLock rwlock) : this(rwlock, Timeout.Infinite) {
		}

		public WriterLock(ReaderWriterLock rwlock, int timeout) {
			acquiredWriteLock = false;
			this.rwlock = rwlock;
			rwlock.AcquireWriterLock(timeout);
			acquiredWriteLock = true;
		}


		#region IDisposable Members

		public void Dispose() {
			if (acquiredWriteLock) {
				rwlock.ReleaseWriterLock();
			}
			else if (upgradedFromReadLock) {
				rwlock.DowngradeFromWriterLock(ref lc);
			}
		}

		#endregion
	}

	internal class ReaderLock : IDisposable {
		private ReaderWriterLock rwlock;
		private bool acquiredReadLock;

		public ReaderLock(ReaderWriterLock rwlock) : this(rwlock, Timeout.Infinite) {
		}

		public ReaderLock(ReaderWriterLock rwlock, int timeout) {
			acquiredReadLock = false;
			this.rwlock = rwlock;
			rwlock.AcquireReaderLock(timeout);
			acquiredReadLock = true;
		}

		public WriterLock UpgradeToWriter() {
			return UpgradeToWriter(Timeout.Infinite);
		}

		public WriterLock UpgradeToWriter(int timeout) {
			return new WriterLock(rwlock.UpgradeToWriterLock(timeout), rwlock);
		}

		#region IDisposable Members

		public void Dispose() {
			if (acquiredReadLock)
				rwlock.ReleaseReaderLock();
		}

		#endregion
	}
}
