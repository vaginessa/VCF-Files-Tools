/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Contacts.Net")]
[assembly: AssemblyDescription("Windows Contacts Managed APIs")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Microsoft")]
[assembly: AssemblyProduct("Microsoft.Communications.Contacts")]
[assembly: AssemblyCopyright("Copyright Microsoft Corporation. All Rights Reserved.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: CLSCompliant(true)]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("15b31678-16c4-496c-98c5-4644bdadacb9")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
[assembly: AssemblyVersion("0.9.33.0")]
[assembly: AssemblyFileVersion("0.9.26.0")]

// TODO: Add these back before releasing.  Right now it's unnecessary overhead.
[assembly:
    SuppressMessage(
        "Microsoft.Design",
        "CA2210:AssembliesShouldHaveValidStrongNames")]
//[assembly: InternalsVisibleTo("Contacts.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100D7C3F407E209CEC882A9C37DAFCF9F0EB578822AD4C132E4471E32E1D474C17C811288260B811911168A61CA581126995073FFA3D65765EA706ECF9D55425CA30EB9F90D19927AF890BDFCB8CBA95C2390B1F31744747624C875F8FCBF3904BD6432495953F9DADB6A65DD86C236CD78B5904166B339B4B89B933CCDE9B237D4")]
[assembly: InternalsVisibleTo("Contacts.Tests")]
[assembly: NeutralResourcesLanguage("en")]