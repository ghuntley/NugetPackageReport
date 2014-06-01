﻿using NugetPackageReport.Nuget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace NugetPackageReport
{
    public static class Processor
    {
		public static Dictionary<PackageConfig, FeedPackage> FeedPackages = new Dictionary<PackageConfig, FeedPackage>();
		
		public static void ProcessDirectory(string path)
        {
			var feed = new V1FeedContext(new Uri("http://packages.nuget.org/v1/FeedService.svc"));

            var directories = new Stack<string>(20);
            directories.Push(path);

            while (directories.Count > 0)
            {
                var directory = directories.Pop();

                var files = Directory.GetFiles(directory);

                var packagePaths = files.Where(x => x.ToLower().EndsWith("packages.config")).ToList();
	            var project = files.FirstOrDefault(x => x.ToLower().Contains("csproj"));
                
				if (packagePaths.Any())
                {
                    var packages = GetPackages(packagePaths);

                    ProcessPackages(feed, packages, project);
                }

	            if (project != null)
	            {
		            continue;
	            }

	            var subDirectories = Directory.GetDirectories(directory).Where(x => x != "packages" && x != ".nuget");
                    
	            foreach (var dir in subDirectories)
	            {
		            directories.Push(dir);
	            }
            }
        }

        private static void ProcessPackages(V1FeedContext feed, IEnumerable<PackageConfig> packages, string projectPath)
        {
	        var projectFileName = Path.GetFileNameWithoutExtension(projectPath);

            foreach (var package in packages)
            {
				var key = FeedPackages.Keys.FirstOrDefault(x => x.ID == package.ID && x.Version == package.Version);

	            if (key != null)
	            {
		            FeedPackages[key].ProjectNames.Add(projectFileName);
	            }
	            else
	            {
		            var package1 = package;
		            var feedPackages = feed.Packages.Where(x => x.Id == package1.ID).ToList();

		            var currentVersion = feedPackages.FirstOrDefault(x => x.Version == package1.Version);

		            var latestVersion =
			            feedPackages.OrderByDescending(x => x, new FeedPackageVersionComparer()).FirstOrDefault();

		            FeedPackages.Add(package, new FeedPackage
		            {
			            CurrentVersion = currentVersion, 
			            LatestVersion = latestVersion,
			            ProjectNames = new List<string>
			            {
				            projectFileName
			            },

		            });
	            }
            }
        }

        private static IEnumerable<PackageConfig> GetPackages(IEnumerable<string> packagePaths)
        {
            var packages = new List<PackageConfig>();

            foreach (var packagePath in packagePaths)
            {
                var doc = XDocument.Load(packagePath);

                var children = doc.Elements().Elements();

                packages = children.Select(x => new PackageConfig
                {
                    ID = x.Attribute("id").Value,
                    Version = x.Attribute("version").Value,
                }).ToList();
            }
            return packages;
        }     
    }
}
