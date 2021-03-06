﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace BandcampDownloader {

    internal static class BandcampHelper {
        /// <summary>
        /// Retrieves the data on the album of the specified Bandcamp page.
        /// </summary>
        /// <param name="htmlCode">The HTML source code of a Bandcamp album page.</param>
        /// <returns>The data on the album of the specified Bandcamp page.</returns>
        public static Album GetAlbum(String htmlCode) {
            // Keep the interesting part of htmlCode only
            String albumData;
            try {
                albumData = GetAlbumData(htmlCode);
            } catch (Exception e) {
                throw new Exception("Could not retrieve album data in HTML code.", e);
            }

            // Fix some wrongly formatted JSON in source code
            albumData = FixJson(albumData);

            // Deserialize JSON
            Album album;
            try {
                album = JsonConvert.DeserializeObject<JsonAlbum>(albumData).ToAlbum();
            } catch (Exception e) {
                throw new Exception("Could not deserialize JSON data.", e);
            }

            return album;
        }

        /// <summary>
        /// Retrieves all the albums URL existing on the specified Bandcamp page.
        /// </summary>
        /// <param name="htmlCode">The HTML source code of a Bandcamp page.</param>
        /// <returns>The albums URL existing on the specified Bandcamp page.</returns>
        public static List<String> GetAlbumsUrl(String htmlCode) {
            // Get artist bandcamp page
            var regex = new Regex("band_url = \"(?<url>.*)\"");
            if (!regex.IsMatch(htmlCode)) {
                throw new NoAlbumFoundException();
            }
            String artistPage = regex.Match(htmlCode).Groups["url"].Value;

            // Get albums relative urls
            regex = new Regex("href=\"(?<url>/album/.*)\"");
            if (!regex.IsMatch(htmlCode)) {
                throw new NoAlbumFoundException();
            }

            var albumsUrl = new List<String>();
            foreach (Match m in regex.Matches(htmlCode)) {
                albumsUrl.Add(artistPage + m.Groups["url"].Value);
            }

            // Remove duplicates
            albumsUrl = albumsUrl.Distinct().ToList();
            return albumsUrl;
        }

        private static String FixJson(String albumData) {
            // Some JSON is not correctly formatted in bandcamp pages, so it needs to be fixed before we can deserialize it

            // In trackinfo property, we have for instance:
            //     url: "http://verbalclick.bandcamp.com" + "/album/404"
            // -> Remove the " + "
            var regex = new Regex("(?<root>url: \".+)\" \\+ \"(?<album>.+\",)");
            String fixedData = regex.Replace(albumData, "${root}${album}");

            return fixedData;
        }

        private static String GetAlbumData(String htmlCode) {
            String startString = "var TralbumData = {";
            String stopString = "};";

            if (htmlCode.IndexOf(startString) == -1) {
                // Could not find startString
                throw new Exception("Could not find the following string in HTML code: var TralbumData = {");
            }

            String albumDataTemp = htmlCode.Substring(htmlCode.IndexOf(startString) + startString.Length - 1);
            String albumData = albumDataTemp.Substring(0, albumDataTemp.IndexOf(stopString) + 1);

            return albumData;
        }
    }
}