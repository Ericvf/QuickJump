using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace QuickJump.Providers
{
    public class EdgeBookmarksProvider : IItemsProvider
    {
        public string Name => nameof(EdgeBookmarksProvider);

        public bool LoadDataOnActivate => false;

        public async Task GetItems(Func<Item, Task> value, CancellationToken cancellationToken)
        {
            var profileNames = GetProfileNames();

            foreach (var profileName in profileNames)
            {
                var edgeBookmarksPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Edge\User Data", profileName, "Bookmarks");

                if (File.Exists(edgeBookmarksPath))
                {
                    using var fileStream = File.OpenRead(edgeBookmarksPath);

                    var bookmarksData = await JsonNode.ParseAsync(fileStream);
                    var bookmarks = ExtractBookmarks(bookmarksData["roots"]["bookmark_bar"]);

                    foreach (var bookmark in bookmarks)
                    {
                        var item = new Item()
                        {
                            Provider = Name,
                            Name = bookmark.Name,
                            Category = Categories.Bookmark,
                            Description = $"{bookmark.Name} {bookmark.Url} ({profileName})",
                            Id = $"{profileName}_{bookmark.Name}_{bookmark.Url}",
                            Type = Types.Uri,
                            Icon = "edge",
                            Path = bookmark.Url,
                        };

                        await value(item);
                    }
                }
            }
        }

        private static IEnumerable<Bookmark> ExtractBookmarks(JsonNode node)
        {
            var bookmarksList = new List<Bookmark>();

            foreach (JsonNode item in node["children"].AsArray())
            {
                if (item["type"].ToString() == "url")
                {
                    bookmarksList.Add(new Bookmark { Name = item["name"].ToString(), Url = item["url"].ToString() });
                }
                else if (item["type"].ToString() == "folder")
                {
                    bookmarksList.AddRange(ExtractBookmarks(item));
                }
            }

            return bookmarksList;
        }

        private static IEnumerable<string> GetProfileNames()
        {
            const string ProfileKeyPath = @"Software\Microsoft\Edge\Profiles";

            using var profilesKey = Registry.CurrentUser.OpenSubKey(ProfileKeyPath);
            if (profilesKey != null)
            {
                foreach (string subKeyName in profilesKey.GetSubKeyNames())
                {
                    using (var profileKey = profilesKey.OpenSubKey(subKeyName))
                    {
                        yield return subKeyName;
                    }
                }
            }
        }

        private class Bookmark
        {
            public string Name { get; set; }
            public string Url { get; set; }
        }
    }
}
