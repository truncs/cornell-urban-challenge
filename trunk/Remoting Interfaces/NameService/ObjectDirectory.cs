using System;
using System.Collections.Generic;

namespace UrbanChallenge.NameService {

	// A NameService for .NET Remoting in the style of the CORBA Nameservice. It acts as a
	// match-maker for remote reference lookup, and allows to change the physical location
	// of any system components without the need to update configuration files or code at
	// dependant components.
	public abstract class ObjectDirectory : MarshalByRefObject {

		// Binds the object to the given name.
		public abstract void Bind(MarshalByRefObject obj, string name);

		// Unbinds the object with the given name.
		public abstract void Unbind(string name);

		// Unbinds an existing object for the given name (if any), and binds
		// the name to the new object reference.
		public abstract void Rebind(MarshalByRefObject obj, string name);

		// Tests whether an object with the given name is already in the directory.
		public abstract bool Contains(string name);

		// Returns the object bound to the given name.
		public abstract MarshalByRefObject Resolve(string name);

		// Returns an array of all object names in the directory.
		public abstract ICollection<string> GetNames();

	}

}
