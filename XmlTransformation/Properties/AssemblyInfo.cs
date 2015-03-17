using System.Reflection;
using System.Runtime.InteropServices;

//
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
[assembly: AssemblyTitle("Sdl.Tms.Ps.XmlTransformation")]
[assembly: AssemblyDescription("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("1284C0A1-B22B-4FE1-8DA5-84BAA368DE53")]

// 1.1.0.0 -- Adding Rename Attribute Class
// 1.1.0.1 -- ??
// 1.1.0.2 -- Adding repeat runMode
// 1.1.3.0 -- ?
// 1.1.4.0 -- Adding extra arguments to values to support XSLT Transformation (Kiet)
// 1.1.5.0 -- Adding ConvertEntities class to convert entitites inside CDATA 
//            to literals (and vice versa) if the input file is either UTF-8 or UTF-16 
//            (Kiet - Jun 1, 2011)
// 1.1.5.1 -- Update Add class with addAfter option - Kiet Jun 3, 2011
// 1.1.5.2 -- Updated ConvertEntities - looping through for whitespace nodes - Kiet Jul 17, 2011
// 1.1.5.3 -- Added CopyAt, InsertAt and AddAt. Consolidated code in Add
// 1.35.5.4 -- Added "System.Xml.XPathNodelist" alongside "System.Xml.XPath.XPathNodelist" & "System.Xml.XmlNodelist"
//          -- Changed assembly version name too
// 1.35.5.5 -- Consolidated Entity management (Eric)
// 1.35.5.6 -- fixed the default namespace issue affecting the addition of new nodes
// 1.35.5.7 -- added optional skipping of proprietary entity escaping/restoring
// 1.40.5.7 -- migratio to .NET 4.0
[assembly: AssemblyInformationalVersion("1.40.5.7")]