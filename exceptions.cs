using System;

namespace Iotbcdg.Exceptions
{
    public class EntryAlreadyExists : Exception
    {
        public EntryAlreadyExists() { }

        public EntryAlreadyExists(string message) : base(message) { }
    }

    public class EntryDoesNotExist : Exception
    {
        public EntryDoesNotExist() { }

        public EntryDoesNotExist(string message) : base(message) { }
    }
}