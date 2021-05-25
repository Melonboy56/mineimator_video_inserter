using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Melonsboy
{
    class animated_texture_inserter
    {
        private static String projectfile = null;
        private static String imagespath = null;
        private static String objectID = null;
        private static String images_folder_name = null;
        private static int keyframestart = 0;

        private static List<String> usedIDs = new List<string>();
        private static Dictionary<int, String> frameorder = new Dictionary<int, string>();
        static void Main(String[] args)
        {
            Console.WriteLine("Animated Texture inserter, By Melonsboy");
            Console.WriteLine("");
            if (args.Length < 4 || get_arg_value(args, "/p") == null || get_arg_value(args, "/i") == null)
            {
                Console.WriteLine("No arguments are passed to this program");
                Console.WriteLine("Example: program.exe /p (Path to project file, use quotes if needed) /i (Path to images folder, use quotes if needed) /id (ID of the object) /k (keyframe for first image [default is 0]) /if (custom images folder name)");
                Console.WriteLine("Required arguments: /p, /i, /id");
                return;
            }
            projectfile = get_arg_value(args, "/p");
            imagespath = get_arg_value(args, "/i");
            images_folder_name = get_arg_value(args, "/if");
            objectID = get_arg_value(args, "/id");
            int ignoreMe = 0;
            // attempts to load the value for argument "/k"
            if (int.TryParse(get_arg_value(args, "/k"), out ignoreMe))
            {
                keyframestart = int.Parse(get_arg_value(args, "/k"));
            }
            else if (!(get_arg_value(args, "/k") == null))
            {
                Console.WriteLine("Error: " + get_arg_value(args, "/k") + " is not a vaild number");
                return;
            }
            // parses the file into json and into memory
            Console.WriteLine("Reading Project file");
            JObject projectjson;
            using (StreamReader file = File.OpenText(projectfile))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                projectjson = (JObject)JToken.ReadFrom(reader);
            }
            Console.WriteLine("Loading used IDs");
            // adds already used IDs into a array list
            int selectedIDindex = -1;
            foreach (var getID in projectjson["resources"])
            {
                usedIDs.Add((string)getID["id"]);
            }
            int indexcount = 0;
            foreach (var getID in projectjson["timelines"])
            {
                usedIDs.Add((string)getID["id"]);
                if ((string)getID["id"]==objectID)
                {
                    selectedIDindex = indexcount;
                }
                indexcount++;
            }
            foreach (var getID in projectjson["templates"])
            {
                usedIDs.Add((string)getID["id"]);
            }
            if (selectedIDindex==-1)
            {
                Console.WriteLine("Invaild object ID, please check if the object ID is correct and is in the program arguments");
                return;
            }
            Console.WriteLine("");
            Console.WriteLine("--= Project settings =--");
            Console.WriteLine("Project tempo is "+projectjson["project"]["tempo"]);
            Console.WriteLine("--====================--");
            Console.WriteLine("Copying images...");
            string foldername = "video1";
            // checks if custom folder name is set
            if (!(images_folder_name == null))
            {
                // custom name is set
                foldername = images_folder_name;
            }
            // checks if the folder already exists
            if (Directory.Exists(Path.GetDirectoryName(projectfile)+"\\"+foldername))
            {
                // directory exists
                if (!(images_folder_name == null))
                {
                    Console.WriteLine("'" + foldername + "' is already a used folder name, please choose a different folder name");
                    return;
                } else
                {
                    bool making_alt_names = true;
                    int videonameint = 1;
                    while (making_alt_names)
                    {
                        if (Directory.Exists(Path.GetDirectoryName(projectfile)+"\\video"+videonameint))
                        {
                            videonameint++;
                        } else
                        {
                            making_alt_names = false;
                        }
                    }
                    foldername = "video" + videonameint;
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(projectfile) + "\\" + foldername);
            // starts copying the files
            string[] files = System.IO.Directory.GetFiles(imagespath);

            // Copy the files and overwrite destination files if they already exist.
            string fileName = string.Empty;
            string destFile = string.Empty;
            int framecount = 0;
            foreach (string s in files)
            {
                // Use static Path methods to extract only the file name from the path.
                fileName = Path.GetFileName(s);
                destFile = Path.Combine(Path.GetDirectoryName(projectfile)+"\\"+foldername, fileName);
                File.Copy(s, destFile, true);
                frameorder.Add(framecount, Path.GetFileName(s));
                framecount++;
            }
            Console.WriteLine("Writing data to project file: writing data to resources section");
            JArray resources_array = (JArray)projectjson["resources"];
            foreach (int s in frameorder.Keys)
            {
                JObject resource_json = JObject.Parse("{}");
                resource_json["id"] = foldername+"_"+s;
                resource_json["type"] = "texture";
                resource_json["filename"] = foldername + "\\" + frameorder[s];
                resources_array.Add(resource_json);
            }
            Console.WriteLine("Writing data to project file: writing data to requested object ID");
            foreach (int s in frameorder.Keys)
            {
                projectjson["timelines"][selectedIDindex]["keyframes"][""+(s+keyframestart)] = JObject.Parse("{}");
                projectjson["timelines"][selectedIDindex]["keyframes"][""+(s+keyframestart)]["TEXTURE_OBJ"] = foldername + "_" + s;
            }

            Console.WriteLine("Saving changes...");
            File.WriteAllText(projectfile, projectjson.ToString());
            Console.WriteLine("Finished inserting images to mineimator project");
            // Console.WriteLine(projectjson);
        }
        static string get_arg_value(String[] process_args, String arginput)
        {
            for (int i = 0; i < process_args.Length; i++)
            {
                if (process_args[i].ToLower().Equals(arginput.ToLower()))
                {
                    if (i == process_args.Length - 1) { return null; }
                    return process_args[i + 1];
                }
            }
            return null;
        }
    }
}