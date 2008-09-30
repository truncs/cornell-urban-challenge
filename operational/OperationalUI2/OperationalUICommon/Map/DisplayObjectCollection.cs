using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.OperationalUI.Common.Map {
	public class DisplayObjectCollection : ICollection<IRenderable> {
		private class DisplayObjectEntry {
			public IRenderable displayObject;
			public bool visible;

			public DisplayObjectEntry(IRenderable displayObject, bool visible) {
				this.displayObject = displayObject;
				this.visible = visible;
			}
		}

		public event EventHandler<DisplayObjectEventArgs> DisplayObjectVisibleChanged;
		public event EventHandler DisplayObjectZOrderChanged;
		public event EventHandler<DisplayObjectEventArgs> DisplayObjectAdded;
		public event EventHandler<DisplayObjectEventArgs> DisplayObjectRemoved;

		private List<DisplayObjectEntry> displayObjects;

		public DisplayObjectCollection() {
			displayObjects = new List<DisplayObjectEntry>();
		}

		public void Add(IRenderable displayObject, bool visible) {
			if (!Contains(displayObject)) {
				displayObjects.Add(new DisplayObjectEntry(displayObject, visible));

				if (DisplayObjectAdded != null) {
					DisplayObjectAdded(this, new DisplayObjectEventArgs(displayObject));
				}
			}
		}

		public void MoveUp(IRenderable displayObject) {
			int index = IndexOf(displayObject);
			if (index != -1 && index < displayObjects.Count-1) {
				// swap the entries
				DisplayObjectEntry temp = displayObjects[index];
				displayObjects[index] = displayObjects[index+1];
				displayObjects[index+1] = temp;

				OnZOrderChanged();
			}
		}

		public void MoveDown(IRenderable displayObject) {
			// find the entry
			int index = IndexOf(displayObject);
			if (index > 0) {
				// swap the entries
				DisplayObjectEntry temp = displayObjects[index];
				displayObjects[index] = displayObjects[index-1];
				displayObjects[index-1] = temp;

				OnZOrderChanged();
			}
		}

		/// <summary>
		/// Moves a display object (source) to before another display object (target)
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target">Specify null to move to end</param>
		public void MoveToBefore(IRenderable source, IRenderable target) {
			int sourceIndex = IndexOf(source);

			int targetIndex = -2;
			if (target != null) {
				targetIndex = IndexOf(target);
			}

			if (sourceIndex == -1 || targetIndex == -1 || sourceIndex == targetIndex)
				return;

			DisplayObjectEntry temp = displayObjects[sourceIndex];
			displayObjects.RemoveAt(sourceIndex);

			if (targetIndex == -2) {
				displayObjects.Add(temp);
			}
			else if (targetIndex < sourceIndex) {
				displayObjects.Insert(targetIndex, temp);
			}
			else {
				displayObjects.Insert(targetIndex-1, temp);
			}

			OnZOrderChanged();
		}

		public void MoveToTop(IRenderable source) {
			int sourceIndex = IndexOf(source);
			if (sourceIndex != -1) {
				DisplayObjectEntry temp = displayObjects[sourceIndex];
				displayObjects.RemoveAt(sourceIndex);
				displayObjects.Add(temp);

				OnZOrderChanged();
			}
		}

		public void MoveToBottom(IRenderable source) {
			int sourceIndex = IndexOf(source);
			if (sourceIndex != -1) {
				DisplayObjectEntry temp = displayObjects[sourceIndex];
				displayObjects.RemoveAt(sourceIndex);
				displayObjects.Insert(0, temp);

				OnZOrderChanged();
			}
		}

		private void OnZOrderChanged() {
			if (DisplayObjectZOrderChanged != null) {
				DisplayObjectZOrderChanged(this, EventArgs.Empty);
			}
		}

		private void OnVisibleChanged(IRenderable obj) {
			if (DisplayObjectVisibleChanged != null) {
				DisplayObjectVisibleChanged(this, new DisplayObjectEventArgs(obj));
			}
		}

		private void OnObjectRemoved(IRenderable obj) {
			if (DisplayObjectRemoved != null) {
				DisplayObjectRemoved(this, new DisplayObjectEventArgs(obj));
			}
		}

		public IRenderable this[int index] {
			get { return displayObjects[index].displayObject; }
		}

		public IRenderable this[string name] {
			get {
				DisplayObjectEntry entry = displayObjects.Find(delegate(DisplayObjectEntry ent) { return ent.displayObject.Name == name; });
				if (entry != null) {
					return entry.displayObject;
				}
				else {
					return null;
				}
			}
		}

		public bool IsVisible(int index) {
			return displayObjects[index].visible;
		}

		public bool IsVisible(IRenderable obj) {
			return displayObjects[IndexOf(obj)].visible;
		}

		public void SetVisible(int index, bool visible) {
			displayObjects[index].visible = visible;
			OnVisibleChanged(displayObjects[index].displayObject);
		}

		public void SetVisible(IRenderable obj, bool visible) {
			int index = IndexOf(obj);
			if (index != -1) {
				displayObjects[index].visible = visible;
				OnVisibleChanged(obj);
			}
		}

		public int IndexOf(IRenderable obj) {
			if (obj == null) {
				return -1;
			}

			return displayObjects.FindIndex(delegate(DisplayObjectEntry ent) { return ent.displayObject == obj; });
		}

		#region ICollection<IRenderable> Members

		void ICollection<IRenderable>.Add(IRenderable item) {
			Add(item, true);
		}

		void ICollection<IRenderable>.Clear() {
			throw new NotSupportedException();
		}

		public bool Contains(IRenderable item) {
			return IndexOf(item) != -1;
		}

		void ICollection<IRenderable>.CopyTo(IRenderable[] array, int arrayIndex) {
			throw new NotSupportedException();
		}

		public int Count {
			get { return displayObjects.Count; }
		}

		bool ICollection<IRenderable>.IsReadOnly {
			get { return false; }
		}

		public bool Remove(IRenderable item) {
			int index = IndexOf(item);
			if (index != -1) {
				displayObjects.RemoveAt(index);
				OnObjectRemoved(item);
				return true;
			}
			return false;
		}

		#endregion

		#region IEnumerable<IRenderable> Members

		public IEnumerator<IRenderable> GetEnumerator() {
			return EnumeratorHelper().GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion

		private IEnumerable<IRenderable> EnumeratorHelper() {
			foreach (DisplayObjectEntry ent in displayObjects) {
				yield return ent.displayObject;
			}
		}

		public IEnumerable<IRenderable> GetVisibleEnumerator() {
			foreach (DisplayObjectEntry ent in displayObjects) {
				if (ent.visible) {
					yield return ent.displayObject;
				}
			}
		}

		public IEnumerable<IRenderable> GetReverseEnumerator() {
			for (int i = displayObjects.Count-1; i >= 0; i--) {
				yield return displayObjects[i].displayObject;
			}
		}

		public IEnumerable<IRenderable> GetReverseVisibleEnumerator() {
			for (int i = displayObjects.Count-1; i >= 0; i--) {
				if (displayObjects[i].visible) {
					yield return displayObjects[i].displayObject;
				}
			}
		}
	}
}
