// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.DotNet.Cli.CliSimplify;

namespace Microsoft.DotNet.Cli.commands.dotnet_add2
{
    public class Add2 : ICommand
    {
        [JsonPropertyName("project")]
        public string Project { get; set; }
        //[JsonPropertyName("subCmd")]
        //public Add2SubCmd SubCommand { get; set; }
        [JsonPropertyName("package")]
        public AddPackage Package { get; set; }
        [JsonPropertyName("reference")]
        public AddReference Reference { get; set; }

        private IEnumerable<ICommand> subCommands => [ Package, Reference ];

        public void Execute()
        {
            subCommands.FirstOrDefault(sb => sb != null)?.Execute();
        }

        public string Parse(string[] args)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < args.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        switch (args[i])
                        {
                            case "package":
                            case "reference":
                                sb.Append($"\"{args[i]}\": {{");
                                break;
                            default:

                                break;
                        }
                        break;
                    case 1:
                        break;
                    case 2:
                        break;
                }
            }
        }
    }

    //public class Add2SubCmd
    //{
    //    [JsonPropertyName("package")]
    //    public AddPackage Package { get; set; }
    //    [JsonPropertyName("reference")]
    //    public AddReference Reference { get; set; }
    //}

    public class AddPackage : ICommand
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("version")]
        public Version Version { get; set; }

        public void Execute()
        {

        }
    }

    public class AddReference : ICommand
    {
        public void Execute()
        {

        }
    }
}
