using PeachPied.WordPress.Build.Plugin;
using System;
using System.Collections.Generic;
using Xunit;

namespace Build.Plugin.Tests
{
    /// <summary></summary>
    public class WpPluginTaskTest
    {
        /// <summary></summary>
        [Fact]
        public void TestTags()
        {
            Assert.Equal("test,tags", WpPluginTask.NormalizeTagList("Test ,  Tags  ,,"));
            Assert.Equal("some tag", WpPluginTask.NormalizeTagList(",some tag"));
        }

        /// <summary></summary>
        [Fact]
        public void TestReadmeTxt()
        {
            var meta = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            var sections = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            //
            Assert.True(WpPluginTask.TryParseReadmeTxt(@"
=== Test Title ===
Contributors: Some Name
Tags: blog, two-columns,
custom-logo,
 translation-ready
Stable tag: 1.2.3
License URI: http://www.gnu.org/licenses/gpl-2.0.html

== Description ==
Test Template
testing purposes

This theme has long description.
".Split('\n'), meta, sections, out var description));

            Assert.Contains("tags", (IDictionary<string, string>)meta);
            Assert.Equal("Test Template\ntesting purposes", description);
        }
    }
}
