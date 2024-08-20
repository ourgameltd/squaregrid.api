using HttpMultipartParser;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;
using SquareGrid.Common.Models;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace SquareGrid.Api.Utils
{
    public class Validated<T> where T : class
    {
        public Validated(T? body)
        {
            Body = body;
        }

        public Validated(HttpResponseData? httpResponseData, T? body = null) : this(body)
        {
            HttpResponseData = httpResponseData;
        }

        public HttpResponseData? HttpResponseData { get; }

        public bool IsValid => HttpResponseData == null;

        public T? Body { get; }
    }

    public static class HttpRequestDataX
    {
        public static async Task<Validated<T>> GetFromStringValidated<T>(this HttpRequestData req, MultipartFormDataParser parser) where T : class
        {
            var json = parser.Parameters.FirstOrDefault(i => i.Name == "json")?.Data;
            if (string.IsNullOrWhiteSpace(json))
            {
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteStringAsync("No json data in body");
                return new Validated<T>(response);
            }

            var data = JsonConvert.DeserializeObject<T>(json);

            if (data == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("No data posted");
                return new Validated<T>(badResponse, data);
            }

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(data, serviceProvider: null, items: null);
            bool isValid = Validator.TryValidateObject(data, validationContext, validationResults, true);

            if (!isValid)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                foreach (var validationResult in validationResults)
                {
                    await badResponse.WriteStringAsync(validationResult.ErrorMessage + "\n");
                }

                return new Validated<T>(badResponse, data); ;
            }

            return new Validated<T>(data);
        }

        public static async Task<Validated<T>> GetFromBodyValidated<T>(this HttpRequestData req) where T : class
        {
            var body = await req.ReadAsStringAsync();
            T? data = await req.ReadFromJsonAsync<T>();

            if (data == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("No data posted");
                return new Validated<T>(badResponse, data);
            }

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(data, serviceProvider: null, items: null);
            bool isValid = Validator.TryValidateObject(data, validationContext, validationResults, true);

            if (!isValid)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                foreach (var validationResult in validationResults)
                {
                    await badResponse.WriteStringAsync(validationResult.ErrorMessage + "\n");
                }

                return new Validated<T>(badResponse, data); ;
            }

            return new Validated<T>(data);
        }
    }
}
