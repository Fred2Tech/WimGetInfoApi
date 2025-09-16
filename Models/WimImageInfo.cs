using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WimGetInfoApi.Models
{
    /// <summary>
    /// Detailed information of a WIM image
    /// </summary>
    public class WimImageInfo
    {
        /// <summary>Image index in the WIM file</summary>
        public int Index { get; set; }
        /// <summary>Image name</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Image description</summary>
        public string? Description { get; set; }
        /// <summary>Size in MB</summary>
        public long SizeMB { get; set; }
        /// <summary>Size in bytes</summary>
        public long SizeBytes { get; set; }
        /// <summary>Indicates if the image is bootable (yes/no)</summary>
        public string IsBootable { get; set; } = "no";
        /// <summary>Architecture (x64, x86, etc.)</summary>
        public string? Architecture { get; set; }
        /// <summary>HAL (Hardware Abstraction Layer)</summary>
        public string? Hal { get; set; }
        /// <summary>System version</summary>
        public string? Version { get; set; }
        /// <summary>Service Pack build</summary>
        public string? ServicePackBuild { get; set; }
        /// <summary>Service Pack level</summary>
        public int ServicePackLevel { get; set; }
        /// <summary>Installation type</summary>
        public string? Installation { get; set; }
        /// <summary>Product type</summary>
        public string? ProductType { get; set; }
        /// <summary>Product suite</summary>
        public string? ProductSuite { get; set; }
        /// <summary>System root directory</summary>
        public string? SystemRoot { get; set; }
        /// <summary>System edition</summary>
        public string? Edition { get; set; }
        /// <summary>Number of directories</summary>
        public long Directories { get; set; }
        /// <summary>Number of files</summary>
        public long Files { get; set; }
        /// <summary>Creation date</summary>
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? Created { get; set; }
        /// <summary>Modification date</summary>
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? Modified { get; set; }
        /// <summary>Supported languages</summary>
        public string? Languages { get; set; }
    }

    /// <summary>
    /// General information about a WIM file
    /// </summary>
    public class WimFileInfo
    {
        /// <summary>File path</summary>
        public string FilePath { get; set; } = string.Empty;
        /// <summary>File name</summary>
        public string FileName { get; set; } = string.Empty;
        /// <summary>File size</summary>
        public long FileSize { get; set; }
        /// <summary>Number of images in the file</summary>
        public int ImageCount { get; set; }
        /// <summary>Indicates if the file is bootable</summary>
        public string IsBootable { get; set; } = "no";
        /// <summary>File creation date</summary>
        public DateTime Created { get; set; }
        /// <summary>File modification date</summary>
        public DateTime Modified { get; set; }
        /// <summary>List of contained images</summary>
        public List<WimImageInfo> Images { get; set; } = new List<WimImageInfo>();
    }

    /// <summary>
    /// WIM image analysis request
    /// </summary>
    public class WimImageRequest
    {
        /// <summary>Path to the WIM file</summary>
        [Required]
        public string WimFilePath { get; set; } = string.Empty;
        /// <summary>Image index or '*' for all</summary>
        [Required]
        public string ImageIndex { get; set; } = string.Empty;
        /// <summary>Debug mode</summary>
        public bool Debug { get; set; } = false;
    }

    /// <summary>
    /// Standardized API response
    /// </summary>
    /// <typeparam name="T">Type of returned data</typeparam>
    public class WimApiResponse<T>
    {
        /// <summary>Indicates if the operation succeeded</summary>
        public bool Success { get; set; }
        /// <summary>Descriptive message</summary>
        public string Message { get; set; } = string.Empty;
        /// <summary>Returned data</summary>
        public T? Data { get; set; }
        /// <summary>Error details if any</summary>
        public string? ErrorDetails { get; set; }
    }

    /// <summary>
    /// Custom converter to format dates as in reference code
    /// </summary>
    public class CustomDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;
            
            var value = reader.GetString();
            if (DateTime.TryParse(value, out DateTime result))
                return result;
            
            return null;
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteStringValue("Not specified");
            }
            else
            {
                // Format exactly as in reference code
                writer.WriteStringValue(value.Value.ToString("dd/MM/yyyy HH:mm:ss"));
            }
        }
    }
}
