﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Web_Crawler
{
    class Program
    {
        static Dictionary<string, string> targets = new Dictionary<string, string>();
        static Dictionary<string, string> history = new Dictionary<string, string>();
        private static readonly Regex slash_re = new Regex("/{2,}",
                RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        private static readonly Regex re = new Regex("<a.*?href=\"(.*?)\"",
                RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        private static CrawlLogger crawllogger;

        private static Stack<string> _urls = new Stack<string>();
        private static int _maxConcurrency = 200;
        private static int taskCount;
        private static List<Task<string>> _tasks = new List<Task<string>>();

        static void Main()
        {
            var target = "http://leftronic.com";
            taskCount = 0;
            crawllogger = new CrawlLogger("crawlerlogger-log.txt");
            

            if (target.IndexOf("http://") != -1)
            {
                var crawlTask = crawl(target);
                crawlTask.Wait();
                Console.ReadLine();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Please enter a valid URL");
                Console.ResetColor();

                Main();
            }
        }

        static async Task crawl(string target)
        {
            
            _urls.Push(target);
            while (_urls.Count > 0 || _tasks.Count > 0)
            {
                while (taskCount < _maxConcurrency && _urls.Count > 0)
                {
                    _tasks.Add(RequestSite(_urls.Pop()));
                }

                Task<string> t = await Task.WhenAny(_tasks);

                var completed = _tasks.Where(x => x.Status == TaskStatus.RanToCompletion).ToList();
                foreach (var task in completed)
                {
                    getLinks(task.Result, target);
                    _tasks.Remove(task);
                }
            }

            //var key = Console.ReadKey().Key;
            //while (key != ConsoleKey.Q)
            //{
            //    Console.WriteLine("Main loop");
            //    while (_urls.Count > 0)
            //    {
            //        while (taskCount < _maxConcurrency && _urls.Count > 0)
            //        {
            //            _tasks.Add(RequestSite(_urls.Pop()));
            //        }

            //        Task<string> t = await Task.WhenAny(_tasks);
            //        string result = await t;
            //        getLinks(result);
            //        key = Console.ReadKey().Key;
            //    }
            //}
            //Environment.Exit(0);
        }


        static async Task<string> RequestSite(string target)
        {
            string responseString = "";
            try
            {
                history.Add(target, "true");
                HttpClient client = new HttpClient();
                Task<HttpResponseMessage> response_contents = client.GetAsync(target);

                Console.BackgroundColor = ConsoleColor.Blue;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Scanning:");
                Console.ResetColor();
                Console.Write(" " + target + "\n");
                taskCount++;
                HttpResponseMessage response = await response_contents;
                crawllogger.AddLog(target);
                taskCount--;

                try
                {
                    if (response.IsSuccessStatusCode)
                    {
                        // by calling .Result you are performing a synchronous call
                        var responseContent = response.Content;

                        // by calling .Result you are synchronously reading the result
                        responseString = responseContent.ReadAsStringAsync().Result;

                    }
                }
                catch (Exception ex)
                {
                    responseString = "";
                }
            }
            catch (ArgumentException)
            {
                Console.WriteLine("An element with Key = {0} already exists.", target);
                responseString = "";
            }
           
       
           

            getLinks(responseString, target);
            return responseString;
           
        }

        static void getLinks(string body, string target)
        {
            MatchCollection matches = re.Matches(body);

            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    var test = matches;
                    string current_match = "";
                    current_match = match.Groups[1].Value;

                    string newTarget = target + current_match;
                    //Console.WriteLine(match.Groups);



                   // Internal site match
                    if (current_match.FirstOrDefault() == '/')
                    {
                        newTarget = "http://" + slash_re.Replace((target + current_match).Replace("http://", ""), "/");

                        Console.ForegroundColor = ConsoleColor.Green;
                        //Console.WriteLine(newTarget);
                        Console.ResetColor();

                        if (!history.ContainsKey(newTarget))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            //Console.WriteLine("NEW: " + (newTarget));
                            Console.ResetColor();
                            _urls.Push(newTarget);
                        }
                        else if (history.ContainsKey(newTarget))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            //Console.WriteLine("EXISTS: " + (newTarget));
                            Console.ResetColor();
                        }

                    }

                    //external site

                    if (current_match.IndexOf("http") == 0 || current_match.IndexOf("https") == 0)
                    {
                        newTarget = current_match;
                        if (!history.ContainsKey(newTarget))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            //Console.WriteLine("NEW EXTERNAL: " + (newTarget));
                            Console.ResetColor();
                            _urls.Push(newTarget);
                        }
                        else if (history.ContainsKey(newTarget))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            //Console.WriteLine("EXISTING EXTERNAL: " + (newTarget));
                            Console.ResetColor();
                        }
                    }
                }
            }
        }
    }
}



