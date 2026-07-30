using System.Collections.Generic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CodeEditor2.Data
{
    /// <summary>
    /// ParseRuleの定義を管理するクラス
    /// parseRuleDefinitions.yaml ファイルを読み込む
    /// </summary>
    public class ParseRuleDefinition
    {
        public ParseRuleDefinition(Project project)
        {
            this.project = project;
            AbsolutePath = project.GetAbsolutePath("parseRuleDefinitions.yaml");

            if (System.IO.File.Exists(AbsolutePath))
            {
                loadFile();
            }
        }

        private Project project;
        public string AbsolutePath { get; private set; }

        /// <summary>
        /// 定義されたRule一覧 (name -> Rule)
        /// </summary>
        private Dictionary<string, Rule> rules = new Dictionary<string, Rule>();

        public void Reload()
        {
            rules.Clear();
            if (System.IO.File.Exists(AbsolutePath))
            {
                loadFile();
            }
        }

        private void loadFile()
        {
            try
            {
                string yaml = System.IO.File.ReadAllText(AbsolutePath);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var yamlData = deserializer.Deserialize<ParseRuleDefinitionsYaml>(yaml);

                if (yamlData?.Rules != null)
                {
                    foreach (var rule in yamlData.Rules)
                    {
                        if (!string.IsNullOrEmpty(rule.Name))
                        {
                            rules[rule.Name] = rule;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load parseRuleDefinitions.yaml: {ex.Message}");
            }
        }

        /// <summary>
        /// 指定された名前のRuleを取得
        /// </summary>
        /// <param name="name">Rule名</param>
        /// <returns>Rule情報（見つからない場合はnull）</returns>
        public Rule? GetRule(string name)
        {
            if (rules.TryGetValue(name, out var rule))
            {
                return rule;
            }
            return null;
        }

        /// <summary>
        /// 定義があるかチェック
        /// </summary>
        public bool HasDefinition()
        {
            return rules.Count > 0;
        }

        /// <summary>
        /// YAML構造对应的クラス
        /// </summary>
        public class ParseRuleDefinitionsYaml
        {
            public List<Rule>? Rules { get; set; }
        }

        /// <summary>
        /// 個別のRule定義
        /// </summary>
        public class Rule
        {
            public string? Name { get; set; }
            public string? Description { get; set; }
            public RuleOptions? Options { get; set; }
        }

        /// <summary>
        /// Ruleのオプション
        /// </summary>
        public class RuleOptions
        {
            public bool SystemVerilog { get; set; }
            public bool Disabled { get; set; }
            public string? ParseMode { get; set; }
            public Dictionary<string, string>? AdditionalOptions { get; set; }
        }
    }
}
