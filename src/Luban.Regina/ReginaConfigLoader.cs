using System.Text;
using System.Text.Json;
using Luban.RawDefs;
using Luban.Schema;
using Luban.Utils;

namespace Luban;

public class ReginaConfigLoader : IConfigLoader
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();
    
    private string _curDir;

    public ReginaConfigLoader()
    {

    }

    private class Group
    {
        public List<string> Names { get; set; }
        
        public bool Default { get; set; }
    }

    private class SchemaFile
    {
        public string FileName { get; set; }
        
        public string Type { get; set; }
    }

    private class Target
    {
        public string Name { get; set; }
        
        public string Manager { get; set; }
        
        public List<string> Groups { get; set; }
        
        public string TopModule { get; set; }
    }

    private class LubanConf
    {
        public List<Group> Groups { get; set; }
        
        public List<SchemaFile> SchemaFiles { get; set; }
        
        public string DataDir { get; set; }
        
        public List<Target> Targets { get; set; }
    }

    public LubanConfig Load(string fileName)
    {
        s_logger.Debug("load config file:{}", fileName);
        _curDir = Directory.GetParent(fileName).FullName;
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        var globalConf = JsonSerializer.Deserialize<LubanConf>(File.ReadAllText(fileName, Encoding.UTF8), options);
        
        List<RawGroup> groups = globalConf.Groups.Select(g => new RawGroup(){ Names = g.Names, IsDefault = g.Default }).ToList();
        List<RawTarget> targets = globalConf.Targets.Select(t => new RawTarget() { Name = t.Name, Manager = t.Manager, Groups = t.Groups, TopModule = t.TopModule }).ToList();
         
        List<SchemaFileInfo> importFiles = new();
        foreach (var schemaFile in globalConf.SchemaFiles)
        {     
            foreach (var subFile in FileUtil.GetFileOrDirectory(Path.Combine(_curDir, schemaFile.FileName)))
            {
                string schemaType = GetSchemaType(subFile);
                importFiles.Add(new SchemaFileInfo(){ FileName = subFile, Type = schemaType});
            }
        }
        return new LubanConfig()
        {
            InputDataDir = Path.Combine(_curDir, globalConf.DataDir),
            Groups = groups,
            Targets = targets,
            Imports = importFiles,
        };
    }

    private string GetSchemaType(string filePath)
    {
        string path = filePath.TrimEnd(Path.GetExtension(filePath).ToCharArray());
        if (filePath.EndsWith("__beans"))
            return "beans";
        else if (filePath.EndsWith("__enums"))
            return "enums";
        else if (filePath.EndsWith("__tables"))
            return "tables";
        return "";
    }
}