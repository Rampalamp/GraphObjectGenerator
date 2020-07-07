using GIBS.Module.Models.Activities;
using GIBS.Module.Models.FastFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GraphObjectGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            //Set desired object type to generate files for.
            Type objType = typeof(FastFileLineItem);
            //save to desktop
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //4 files needed, 1 model file, 1 graph object, 1 repo, 1 interace repo
            string pathOne = String.Format(@"{0}\{1}.cs", desktop, objType.Name);
            string pathTwo = String.Format(@"{0}\{1}Object.cs", desktop, objType.Name);
            string pathThree = String.Format(@"{0}\{1}Repository.cs", desktop, objType.Name);
            string pathFour = String.Format(@"{0}\I{1}Repository.cs", desktop, objType.Name);
            string pathFive = String.Format(@"{0}\{1}InputType.cs", desktop, objType.Name);
            //open all required files/stream writers
            //pathOne makes the Graph Model type
            using (StreamWriter sw = new StreamWriter(pathOne))
            {
                //pathTwo makes the Graph Object type
                using (StreamWriter swTwo = new StreamWriter(pathTwo))
                {
                    //pathThree makes the Graph Repo
                    using (StreamWriter swThree = new StreamWriter(pathThree))
                    {
                        //pathFour makes the Graph interface repo
                        using (StreamWriter swFour = new StreamWriter(pathFour))
                        {
                            using (StreamWriter swFive = new StreamWriter(pathFive))
                            {
                                StringBuilder sb = new StringBuilder();
                                StringBuilder sbTwo = new StringBuilder();
                                StringBuilder sbThree = new StringBuilder();
                                StringBuilder sbFour = new StringBuilder();
                                StringBuilder sbFive = new StringBuilder();
                                //init the file types, model and object can share same init function.
                                InitializeModelFile(sb, objType.Name);
                                InitializeObjectFile(sbTwo, objType.Name);
                                InitializeRepoFile(sbThree, objType.Name);
                                InitializeInterfaceFile(sbFour, objType.Name);
                                InitializeInputFile(sbFive, objType.Name);

                                string lowerVariant = Char.ToLowerInvariant(objType.Name[0]) + objType.Name.Substring(1);
                                //loop properties adding accordingly
                                foreach (PropertyInfo prop in objType.GetProperties())
                                {
                                    //public int AttachmentCount { get; set; }
                                    string type = prop.PropertyType.Name;
                                    string name = prop.Name;
                                    //default to string graph type
                                    string graphType = "StringGraphType";
                                    //skip the undesirable BaseObject properties
                                    if (
                                        name == "This" ||
                                        name == "Loading" ||
                                        name == "Session" ||
                                        name == "ClassInfo" ||
                                        name == "IsLoading" ||
                                        name == "IsDeleted"
                                        )
                                        continue;
                                    //switch to adjust property type where needed.
                                    switch (type)
                                    {
                                        case "Guid":
                                            //no change required
                                            graphType = "IdGraphType";
                                            break;
                                        case "String":
                                            type = "string";
                                            //graphType defaults to string.
                                            break;
                                        case "Boolean":
                                            type = "bool";
                                            graphType = "BooleanGraphType";
                                            break;
                                        case "DateTime":
                                            //no change required for type
                                            graphType = "DateTimeGraphType";
                                            break;
                                        case "Int32":
                                            type = "int";
                                            graphType = "IntGraphType";
                                            break;
                                        case "Double":
                                            type = "double";
                                            //dont think there is a DoubleGraphType
                                            graphType = "DecimalGraphType";
                                            break;
                                        case "Decimal":
                                            type = "decimal";
                                            graphType = "DecimalGraphType";
                                            break;
                                        case "XPCollection`1":
                                            //default to making collections a list of strings (often [Name] value of listed object)
                                            type = "List<string>";
                                            graphType = "ListGraphType<StringGraphType>";
                                            break;
                                        default:
                                            //Xaf business object most likely - insert as a string by default, don't always need to bother making it an object.
                                            type = "string";
                                            //leave graphType defaulted to string.
                                            break;
                                    }
                                    //always public for model type
                                    sb.AppendLine(String.Format("public {0} {1} {{ get; set; }}", type, name));
                                    //set Object field
                                    //this.Field(x => x.Oid, type: typeof(GraphQL.Types.IdGraphType)).Description("ExtendedMeasureDataItem Oid");
                                    sbTwo.AppendLine(String.Format("this.Field(x => x.{0}, type: typeof({1})).Description(\"{2} {0}\");", name, graphType, objType.Name));
                                    //set Input field
                                    //this.Field(x => x.ResidentialApplication, type: typeof(ResidentialAppInputType)).Description("");
                                    sbFive.AppendLine(String.Format("this.Field(x => x.{0}, type: typeof({1})).Description(\"{0} Input Field\");", name, graphType));
                                }
                                //add code snipperts for query/mutations of object into the model file
                                InsertQueryAndMutationCodeSnippets(sb, objType.Name, lowerVariant);
                                InsertDatabaseMethodCodeSnippets(sb, objType.Name, lowerVariant);
                                //finalize .cs file closing brackets.
                                FinalizeFile(sb);
                                FinalizeInputOrObjectFile(sbTwo);
                                FinalizeFile(sbThree);
                                FinalizeFile(sbFour);
                                FinalizeInputOrObjectFile(sbFive);
                                //write out to each stream writer from respective string builder.
                                sw.Write(sb.ToString());
                                swTwo.Write(sbTwo.ToString());
                                swThree.Write(sbThree.ToString());
                                swFour.Write(sbFour.ToString());
                                swFive.Write(sbFive.ToString());
                            }
                        }
                    }
                }
            }
        }

        public static void InitializeModelFile(StringBuilder sb, string objectName)
        {
            sb.AppendLine("using GIBSGraphQLApi.Models;");
            sb.AppendLine("using GIBSGraphQLApi.Repositories;");
            sb.AppendLine("using GraphQL.Types;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("namespace GIBSGraphQLApi.Models");
            sb.AppendLine("{");
            sb.AppendLine(String.Format("public class {0}", objectName));
            sb.AppendLine("{");
        }

        public static void InitializeObjectFile(StringBuilder sb, string objectName)
        {
            sb.AppendLine("using GIBSGraphQLApi.Models;");
            sb.AppendLine("using GIBSGraphQLApi.Repositories;");
            sb.AppendLine("using GraphQL.Types;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("namespace GIBSGraphQLApi.Models");
            sb.AppendLine("{");
            sb.AppendLine(String.Format("public class {0}Object : ObjectGraphType<{0}>", objectName));
            sb.AppendLine("{");
            sb.AppendLine(String.Format("public {0}Object(I{0}Repository {1}Repository)", objectName, Char.ToLowerInvariant(objectName[0]) + objectName.Substring(1)));
            sb.AppendLine("{");
            sb.AppendLine(String.Format("this.Name = \"{0}\";", objectName));
            sb.AppendLine(String.Format("this.Description = \"{0} GIBS Business Object\";", objectName));
        }

        public static void InitializeRepoFile(StringBuilder sb, string objectName)
        {
            sb.AppendLine("using GIBSGraphQLApi.Models;");
            sb.AppendLine("using GIBSGraphQLApi.Repositories;");
            sb.AppendLine("using GraphQL.Types;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Threading;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("namespace GIBSGraphQLApi.Repositories");
            sb.AppendLine("{");
            sb.AppendLine(String.Format("public class {0}Repository : I{0}Repository", objectName));
            sb.AppendLine("{");
            sb.AppendLine(String.Format("public Task<{0}> Get{0}(Guid id, CancellationToken cancellationToken) => Task.FromResult(Database.Get{0}(id));", objectName));
            sb.AppendLine(String.Format("public Task<List<{0}>> Get{0}s(CancellationToken cancellationToken) => Task.FromResult(Database.Get{0}s());", objectName));
            sb.AppendLine(String.Format("public Task<{0}> Update{0}({0} input, CancellationToken cancellationToken) => Task.FromResult(Database.Update{0}(input));", objectName));

        }

        public static void InitializeInterfaceFile(StringBuilder sb, string objectName)
        {

            sb.AppendLine("using GIBSGraphQLApi.Models;");
            sb.AppendLine("using GIBSGraphQLApi.Repositories;");
            sb.AppendLine("using GraphQL.Types;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Threading;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("namespace GIBSGraphQLApi.Repositories");
            sb.AppendLine("{");
            sb.AppendLine(String.Format("public interface I{0}Repository", objectName));
            sb.AppendLine("{");
            sb.AppendLine(String.Format("Task<List<{0}>> Get{0}s(CancellationToken cancellationToken);", objectName));
            sb.AppendLine(String.Format("Task<{0}> Get{0}(Guid id, CancellationToken cancellationToken);", objectName));
            sb.AppendLine(String.Format("Task<{0}> Update{0}({0} input, CancellationToken cancellationToken);", objectName));
        }

        public static void InitializeInputFile(StringBuilder sb, string objectName)
        {
            sb.AppendLine("using GIBSGraphQLApi.Models;");
            sb.AppendLine("using GIBSGraphQLApi.Repositories;");
            sb.AppendLine("using GraphQL.Types;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("namespace GIBSGraphQLApi.Types.InputTypeObjects");
            sb.AppendLine("{");
            sb.AppendLine(String.Format("public class {0}InputType : InputObjectGraphType<{0}>", objectName));
            sb.AppendLine("{");
            sb.AppendLine(String.Format("public {0}InputType()", objectName));
            sb.AppendLine("{");
        }

        public static void InsertQueryAndMutationCodeSnippets(StringBuilder sb, string objectName, string lowerVariant)
        {

            //single object grab by Id
            sb.AppendLine("/*");
            sb.AppendLine(String.Format("this.FieldAsync<{0}Object, {0}>(", objectName));
            sb.AppendLine(String.Format("\"{0}\",", objectName));
            sb.AppendLine(String.Format("\"{0} - Fetch GIBS {0} by Oid\",", objectName));
            sb.AppendLine("arguments: new QueryArguments(");
            sb.AppendLine("new QueryArgument<NonNullGraphType<IdGraphType>>()");
            sb.AppendLine("{");
            sb.AppendLine("Name = \"id\",");
            sb.AppendLine(String.Format("Description = \"The unique identifier of the {0}.\",", objectName));
            sb.AppendLine("}),");
            sb.AppendLine(String.Format("resolve: context => {1}Repository.Get{0}(", objectName, lowerVariant));
            sb.AppendLine("context.GetArgument(\"id\", defaultValue: new Guid(\"00000000 - 0000 - 0000 - 0000 - 000000000000\")),");
            sb.AppendLine("context.CancellationToken));");
            sb.AppendLine("*/");
            //connection call for list of objects
            sb.AppendLine("/*");
            sb.AppendLine(String.Format("this.Connection<{0}Object>()", objectName));
            sb.AppendLine(String.Format(".Name(\"{0}s\")", objectName));
            sb.AppendLine(String.Format(".Description(\"Gets GIBS {0}s.\")", objectName));
            sb.AppendLine(".Bidirectional()");
            sb.AppendLine(".PageSize(MaxPageSize)");
            sb.AppendLine(String.Format(".ResolveAsync(context => ResolveConnection({0}Repository, context));", lowerVariant));
            sb.AppendLine("*/");
            //Connection resolver.
            sb.AppendLine("/*");
            sb.AppendLine("private async static Task<object> ResolveConnection(");
            sb.AppendLine(String.Format("I{0}Repository {1}Repository,", objectName, lowerVariant));
            sb.AppendLine("ResolveConnectionContext<object> context)");
            sb.AppendLine("{");
            sb.AppendLine("var first = context.First;");
            sb.AppendLine("var afterCursor = Cursor.FromCursor<DateTime?>(context.After);");
            sb.AppendLine("var last = context.Last;");
            sb.AppendLine("var beforeCursor = Cursor.FromCursor<DateTime?>(context.Before);");
            sb.AppendLine("var cancellationToken = context.CancellationToken;");
            sb.AppendLine(String.Format("var get{0}sTask = await {1}Repository.Get{0}s(cancellationToken);", objectName, lowerVariant));
            sb.AppendLine(String.Format("var {0}s = get{0}sTask;", objectName));
            sb.AppendLine("var hasNextPage = false;");
            sb.AppendLine("var hasPreviousPage = false;");
            sb.AppendLine("var totalCount = 0;");
            sb.AppendLine(String.Format("var (firstCursor, lastCursor) = Cursor.GetFirstAndLastCursor({0}s, x => x.Oid);", objectName));
            sb.AppendLine(String.Format("return new Connection<{0}>()", objectName));
            sb.AppendLine("{");
            sb.AppendLine(String.Format("Edges = {0}s", objectName));
            sb.AppendLine(".Select(x =>");
            sb.AppendLine(String.Format("new Edge<{0}>()", objectName));
            sb.AppendLine("{");
            sb.AppendLine("Cursor = Cursor.ToCursor(x.Oid),");
            sb.AppendLine("Node = x");
            sb.AppendLine("})");
            sb.AppendLine(".ToList(),");
            sb.AppendLine("PageInfo = new PageInfo()");
            sb.AppendLine("{");
            sb.AppendLine("HasNextPage = hasNextPage,");
            sb.AppendLine("HasPreviousPage = hasPreviousPage,");
            sb.AppendLine("StartCursor = firstCursor,");
            sb.AppendLine("EndCursor = lastCursor,");
            sb.AppendLine("},");
            sb.AppendLine("TotalCount = totalCount,");
            sb.AppendLine(" };");
            sb.AppendLine("}");
            sb.AppendLine("*/");
            //MutationObject code snippet
            sb.AppendLine("/*");
            sb.AppendLine(String.Format("this.FieldAsync<{0}Object, {0}>(", objectName));
            sb.AppendLine(String.Format("\"update{0}\",", objectName));
            sb.AppendLine(String.Format("\"Update current {0}.\",", objectName));
            sb.AppendLine("arguments: new QueryArguments(");
            sb.AppendLine(String.Format("new QueryArgument<NonNullGraphType<{0}InputType>>()", objectName));
            sb.AppendLine("{");
            sb.AppendLine("Name = \"inputType\",");
            sb.AppendLine(String.Format("Description = \"{0} input type object.\",", objectName));
            sb.AppendLine("}),");
            sb.AppendLine("resolve: context =>");
            sb.AppendLine("{");
            sb.AppendLine(String.Format("var input = context.GetArgument<{0}>(\"inputType\");", objectName));
            sb.AppendLine(String.Format("return {0}Repository.Update{1}(input, context.CancellationToken);", lowerVariant, objectName));
            sb.AppendLine(" });");
            sb.AppendLine("*/");
        }

        public static void InsertDatabaseMethodCodeSnippets(StringBuilder sb, string objectName, string lowerVariant)
        {
            sb.AppendLine("/*");
            sb.AppendLine(String.Format("public static {0} Update{0}({0} {1}){{return new {0}(); }}", objectName, lowerVariant));
            sb.AppendLine(String.Format("public static {0} Get{0}(Guid id){{ return new {0}(); }}", objectName));
            sb.AppendLine(String.Format("public static List<{0}> Get{0}s(){{ return new List<{0}>(); }}", objectName));
            sb.AppendLine("*/");
        }

        public static void FinalizeFile(StringBuilder sb)
        {
            sb.AppendLine("}");
            sb.AppendLine("}");
        }

        public static void FinalizeInputOrObjectFile(StringBuilder sb)
        {
            sb.AppendLine("}");
            sb.AppendLine("}");
            sb.AppendLine("}");
        }
    }
}
