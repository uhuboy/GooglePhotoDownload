using Google.Apis.Auth.OAuth2;
using Google.Apis.PhotosLibrary.v1;
using Google.Apis.PhotosLibrary.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GooglePhotosDownloader
{
    internal class Program
    {
        // Scopes for the Google Photos API
        static string[] Scopes = { PhotosLibraryService.Scope.PhotoslibraryReadonly };
        static string ApplicationName = "Google Photos Downloader";

        // Hard-coded client ID and client secret
        static string clientId = "XXXXXXXX";
        static string clientSecret = "YYYYYYYY";
        // Specify the download directory
        static string downloadDirectory = @"C:\temp\gphoto";
        static async Task Main(string[] args)
        {
            try
            {
                // Ensure the download directory exists
                Directory.CreateDirectory(downloadDirectory);

                // Authorize the user
                UserCredential credential = await Authorize();

                // Create Google Photos Library API service
                var service = new PhotosLibraryService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                string nextPageToken = null;

                do
                {
                    // Create a search request for media items
                    var searchRequest = new SearchMediaItemsRequest
                    {
                        PageSize = 100, // Increase page size for efficiency
                        PageToken = nextPageToken
                    };

                    // Call the Photos API to search for media items
                    var mediaItemsResponse = await service.MediaItems.Search(searchRequest).ExecuteAsync();

                    if (mediaItemsResponse.MediaItems != null)
                    {
                        foreach (var item in mediaItemsResponse.MediaItems)
                        {
                            string downloadUrl = item.BaseUrl + "=d"; // Append '=d' to download
                            string fileName = item.Filename;

                            // Create full path for the downloaded file
                            string filePath = Path.Combine(downloadDirectory, fileName);

                            Console.WriteLine($"Downloading {fileName}...");

                            using (HttpClient client = new HttpClient())
                            {
                                var response = await client.GetAsync(downloadUrl);

                                if (response.IsSuccessStatusCode)
                                {
                                    byte[] data = await response.Content.ReadAsByteArrayAsync();
                                    await File.WriteAllBytesAsync(filePath, data);
                                    Console.WriteLine($"Downloaded: {filePath}");
                                }
                                else
                                {
                                    Console.WriteLine($"Failed to download {fileName}");
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No media items found.");
                    }

                    // Update the next page token for the next request
                    nextPageToken = mediaItemsResponse.NextPageToken;

                } while (!string.IsNullOrEmpty(nextPageToken)); // Continue if there is a next page
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
        // Method to authorize with hard-coded credentials
        static async Task<UserCredential> Authorize()
        {
            // Manually build the GoogleClientSecrets object with hard-coded client ID and secret
            var clientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };

            // Request authorization and get the token
            return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore("token.json", true));
        }
    }
}
