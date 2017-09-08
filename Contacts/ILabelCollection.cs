/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Communications.Contacts
{
    using System.Collections.Generic;

    public interface ILabelCollection : ICollection<string>
    {
        string PropertyName { get; }

        new bool Add(string item);

        bool AddRange(params string[] items);
    }
}
