/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Communications.Contacts
{
    using System.Text;
    using System.Collections.Generic;
    using System;
    using System.IO;

    internal interface IContactProperties
    {
        string CreateArrayNode(string collectionName, bool appendNode);

        bool DeleteArrayNode(string nodeName);

        bool DeleteProperty(string propertyName);

        bool DoesPropertyExist(string property);

        ContactProperty GetAttributes(string propertyName);

        Stream GetBinary(string propertyName, out string propertyType);

        DateTime? GetDate(string propertyName);

        string GetLabeledNode(string collection, string[] labelFilter);

        IList<string> GetLabels(string node);

        IEnumerable<ContactProperty> GetPropertyCollection(string collectionName, string[] labelFilter, bool anyLabelMatches);

        string GetString(string propertyName);

        bool IsReadonly { get; }

        bool IsUnchanged { get; }

        Stream SaveToStream();

        void SetBinary(string propertyName, Stream value, string valueType);

        void SetDate(string propertyName, DateTime value);

        void SetString(string propertyName, string value);

        void AddLabels(string node, ICollection<string> labels);

        void ClearLabels(string node);

        /// <summary>
        /// Remove a label from the specified array node property.
        /// </summary>
        /// <param name="node">The array node property from which the label is to be removed.</param>
        /// <param name="label">The label to remove.</param>
        /// <returns>
        /// Returns true if the node's label collection was modified as a result of this call.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// If the node or label are either null or empty strings.
        /// If the node name provided is a property in the contact that is not an array node.
        /// </exception>
        bool RemoveLabel(string node, string label);

        string StreamHash { get; }
    }
}
