using System.Linq.Expressions;
using Azure.Data.Tables;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;

namespace VedAstro.Library
{
    /// <summary>
    /// Wrapper API for Google API with backup built in cache
    /// NOTE: all Google API code should only be here
    /// </summary>
    public class Location
    {
        /// <summary>
        /// sample holder type when doing interop
        /// </summary>
        public record GeoLocationRawAPI(dynamic MainRow, dynamic MetadataRow);


        private const string IpAddressToGeoLocationRoute = "IpAddressToGeoLocation";
        private const string AddressToGeoLocationRoute = "AddressToGeoLocation/{address}";
        private const string CoordinatesToGeoLocationRoute = "CoordinatesToGeoLocation/Latitude/{latitude}/Longitude/{longitude}";
        private const string GeoLocationToTimezoneRoute = "GeoLocationToTimezone/{*timeUrl}";

        private readonly TableServiceClient timezoneServiceClient;
        private readonly TableServiceClient timezoneMetadataServiceClient;
        private readonly TableServiceClient addressServiceClient;
        private readonly TableServiceClient addressMetadataServiceClient;
        private readonly TableServiceClient coordinatesServiceClient;
        private readonly TableServiceClient coordinatesMetadataServiceClient;
        private readonly TableServiceClient ipAddressServiceClient;
        private readonly TableServiceClient ipAddressMetadataServiceClient;

        private readonly TableClient timezoneTableClient;
        private readonly TableClient timezoneMetadataTableClient;
        private readonly TableClient addressTableClient;
        private readonly TableClient addressMetadataTableClient;
        private readonly TableClient coordinatesTableClient;
        private readonly TableClient coordinatesMetadataTableClient;
        private readonly TableClient ipAddressTableClient;
        private readonly TableClient ipAddressMetadataTableClient;


        public enum APIProvider
        {
            VedAstro, Azure, Google, IpData
        }


        /// <summary>
        /// init Table access
        /// </summary>
        public Location()
        {
            string accountName = "centralapistorage"; //indic heritage 
            //string accountName = "vedastroapistorage"; //vedastro 

            //# ADDRESS
            //------------------------------------
            //Initialize address table 
            string tableNameAddress = "AddressGeoLocation";

            var storageUriAddress = $"https://{accountName}.table.core.windows.net/{tableNameAddress}";
            //save reference for late use
            addressServiceClient = new TableServiceClient(new Uri(storageUriAddress), new TableSharedKeyCredential(accountName, Secrets.AzureGeoLocationStorageKey));
            addressTableClient = addressServiceClient.GetTableClient(tableNameAddress);

            //Initialize address metadata table 
            string tableNameAddressMetadata = "AddressGeoLocationMetadata";
            var storageUriAddressMetadata = $"https://{accountName}.table.core.windows.net/{tableNameAddressMetadata}";

            //save reference for late use
            addressMetadataServiceClient = new TableServiceClient(new Uri(storageUriAddressMetadata), new TableSharedKeyCredential(accountName, Secrets.AzureGeoLocationStorageKey));
            addressMetadataTableClient = addressMetadataServiceClient.GetTableClient(tableNameAddressMetadata);


            //# COORDINATES
            //------------------------------------
            //Initialize coordinates table 
            string tableNameCoordinates = "CoordinatesGeoLocation";

            var storageUriCoordinates = $"https://{accountName}.table.core.windows.net/{tableNameCoordinates}";
            //save reference for late use
            coordinatesServiceClient = new TableServiceClient(new Uri(storageUriCoordinates), new TableSharedKeyCredential(accountName, Secrets.AzureGeoLocationStorageKey));
            coordinatesTableClient = coordinatesServiceClient.GetTableClient(tableNameCoordinates);

            //Initialize coordinates metadata table 
            string tableNameCoordinatesMetadata = "CoordinatesGeoLocationMetadata";
            var storageUriCoordinatesMetadata = $"https://{accountName}.table.core.windows.net/{tableNameCoordinatesMetadata}";

            //save reference for late use
            coordinatesMetadataServiceClient = new TableServiceClient(new Uri(storageUriCoordinatesMetadata), new TableSharedKeyCredential(accountName, Secrets.AzureGeoLocationStorageKey));
            coordinatesMetadataTableClient = coordinatesMetadataServiceClient.GetTableClient(tableNameCoordinatesMetadata);


            //# IP ADDRESS
            //------------------------------------
            //Initialize address table 
            string tableNameIpAddress = "IpAddressGeoLocation";
            var storageUriIpAddress = $"https://{accountName}.table.core.windows.net/{tableNameIpAddress}";
            //save reference for late use
            ipAddressServiceClient = new TableServiceClient(new Uri(storageUriIpAddress), new TableSharedKeyCredential(accountName, Secrets.AzureGeoLocationStorageKey));
            ipAddressTableClient = ipAddressServiceClient.GetTableClient(tableNameIpAddress);

            //Initialize address metadata table 
            string tableNameIpAddressMetadata = "IpAddressGeoLocationMetadata";
            var storageUriIpAddressMetadata = $"https://{accountName}.table.core.windows.net/{tableNameIpAddressMetadata}";

            //save reference for late use
            ipAddressMetadataServiceClient = new TableServiceClient(new Uri(storageUriIpAddressMetadata), new TableSharedKeyCredential(accountName, Secrets.AzureGeoLocationStorageKey));
            ipAddressMetadataTableClient = ipAddressMetadataServiceClient.GetTableClient(tableNameIpAddressMetadata);


            //# TIMEZONE
            //------------------------------------
            //Initialize timezone table 
            string tableNameTimezone = "GeoLocationTimezone";
            var storageUriTimezone = $"https://{accountName}.table.core.windows.net/{tableNameTimezone}";

            //save reference for late use
            timezoneServiceClient = new TableServiceClient(new Uri(storageUriTimezone), new TableSharedKeyCredential(accountName, Secrets.AzureGeoLocationStorageKey));
            timezoneTableClient = timezoneServiceClient.GetTableClient(tableNameTimezone);

            //Initialize timezone table 
            string tableNameTimezoneMetadata = "GeoLocationTimezoneMetadata";
            var storageUriTimezoneMetadata = $"https://{accountName}.table.core.windows.net/{tableNameTimezoneMetadata}";

            //save reference for late use
            timezoneMetadataServiceClient = new TableServiceClient(new Uri(storageUriTimezoneMetadata), new TableSharedKeyCredential(accountName, Secrets.AzureGeoLocationStorageKey));
            timezoneMetadataTableClient = timezoneMetadataServiceClient.GetTableClient(tableNameTimezoneMetadata);


        }


        /// <summary>
        /// https://vedastroapibeta.azurewebsites.net/api/Calculate/AddressToGeoLocation/Gaithersburg, MD, USA
        /// http://localhost:7071/api/Calculate/AddressToGeoLocation/Gaithersburg, MD, USA
        /// </summary>
        public async Task<GeoLocation> AddressToGeoLocation(string userInputAddressRaw)
        {
            //NOTE: we want to include misspelled and under-case to save
            var userInputAddress = Tools.CleanLocationName(userInputAddressRaw); //possible

            //1 : CALCULATE
            // Define a list of functions in the order of priority
            var geoLocationProviders = new Dictionary<APIProvider, Func<string, Task<GeoLocationRawAPI>>>
            {
                {APIProvider.VedAstro, AddressToGeoLocation_VedAstro},
                {APIProvider.Azure, AddressToGeoLocation_Azure},
                //{APIProvider.Google, AddressToGeoLocation_Google},
            };

            //start with empty as default if fail
            var parsedGeoLocation = GeoLocation.Empty;

            // Iterate over the list of functions
            foreach (var row in geoLocationProviders)
            {
                var provider = row.Value;
                var fullGeoRowData = (await provider(userInputAddress));
                parsedGeoLocation = fullGeoRowData.MainRow.ToGeoLocation(); 


                // when new location not is cache, we add it
                // only add to cache if not empty and not VedAstro
                var isNotEmpty = parsedGeoLocation.Name() != GeoLocation.Empty.Name();
                var apiProvider = row.Key;
                var isNotVedAstro = apiProvider != APIProvider.VedAstro;
                if (isNotEmpty && isNotVedAstro)
                {
                    //add new data to cache, for future speed up
                    AddToAddressTable(fullGeoRowData.MainRow);
                }

                //once found, stop searching for location with APIs
                if (isNotEmpty) { break; }
            }

            //2 : SEND TO CALLER
            return parsedGeoLocation;
        }

        /// <summary>
        /// Will get ip address from caller need to supply IP
        /// https://vedastroapibeta.azurewebsites.net/api/IpAddressToGeoLocation/IpAddress/180.75.241.81
        /// </summary>
        public async Task<GeoLocation> IpAddressToGeoLocation(string ipAddress)
        {

            //1 : CALCULATE
            //if on local, then handle debug ip
#if DEBUG
            ipAddress = ipAddress == "255.255.255.255" ? "180.75.241.81" : ipAddress;
#endif

            // Define a list of functions in the order of priority
            var geoLocationProviders = new Dictionary<APIProvider, Func<string, Task<GeoLocationRawAPI>>>
            {
                {APIProvider.VedAstro, IpAddressToGeoLocation_VedAstro},
                {APIProvider.IpData, IpAddressToGeoLocation_IpData},
            };

            //start with empty as default if fail
            var parsedGeoLocation = GeoLocation.Empty;

            // Iterate over the list of functions
            foreach (var row in geoLocationProviders)
            {
                var provider = row.Value;
                var fullGeoRowData = (await provider(ipAddress));
                parsedGeoLocation = fullGeoRowData.MainRow.ToGeoLocation();

                // when new location not is cache, we add it
                // only add to cache if not empty and not VedAstro
                var isNotEmpty = parsedGeoLocation.Name() != GeoLocation.Empty.Name();
                var apiProvider = row.Key;
                var isNotVedAstro = apiProvider != APIProvider.VedAstro;
                if (isNotEmpty && isNotVedAstro)
                {
                    //add new data to cache, for future speed up
                    AddToIpAddressTable(fullGeoRowData.MainRow);
                    AddToIpAddressMetadataTable(fullGeoRowData.MetadataRow);
                }

                //once found, stop searching for location with APIs
                if (isNotEmpty) { break; }
            }

            //2 : SEND TO CALLER
            return parsedGeoLocation;
        }

        /// <summary>
        /// https://vedastroapibeta.azurewebsites.net/api/CoordinatesToGeoLocation/Latitude/46.9748794/Longitude/8.7843529
        /// </summary>
        public async Task<GeoLocation> CoordinatesToGeoLocation(string latitudeStr, string longitudeStr)
        {

            //0 : ROUND COORDINATES
            //round coordinates to 3 decimal places
            var latitude = Math.Round(double.Parse(latitudeStr), 3);
            var longitude = Math.Round(double.Parse(longitudeStr), 3);


            //1 : CALCULATE
            // Define a list of functions in the order of priority
            var geoLocationProviders = new Dictionary<APIProvider, Func<double, double, Task<GeoLocationRawAPI>>>
            {
                {APIProvider.VedAstro, CoordinatesToGeoLocation_Vedastro},
                {APIProvider.Azure, CoordinatesToGeoLocation_Google},
            };

            //start with empty as default if fail
            var parsedGeoLocation = GeoLocation.Empty;

            // Iterate over the list of functions
            foreach (var row in geoLocationProviders)
            {

                var provider = row.Value;
                var fullGeoRowData = (await provider(longitude, latitude));
                parsedGeoLocation = fullGeoRowData.MainRow.ToGeoLocation();

                // when new location not is cache, we add it
                // only add to cache if not empty and not VedAstro
                var isNotEmpty = parsedGeoLocation.Name() != GeoLocation.Empty.Name();
                var apiProvider = row.Key;
                var isNotVedAstro = apiProvider != APIProvider.VedAstro;
                if (isNotEmpty && isNotVedAstro)
                {
                    //add new data to cache, for future speed up
                    AddToCoordinatesTable(fullGeoRowData.MainRow);
                }

                //once found, stop searching for location with APIs
                if (isNotEmpty) { break; }
            }


            //2 : SEND TO CALLER
            return parsedGeoLocation;
        }

        /// <summary>
        /// https://vedastroapibeta.azurewebsites.net/api/GeoLocationToTimezone/Location/Chennai,TamilNadu,India/Time/23:37/07/08/1990/+01:00
        /// </summary>
        public async Task<string> GeoLocationToTimezone(GeoLocation geoLocation, DateTimeOffset stdTimeAtLocation)
        {
            //2 : CALCULATE
            // Define a list of functions in the order of priority
            var geoLocationProviders = new Dictionary<APIProvider, Func<GeoLocation, DateTimeOffset, Task<GeoLocationRawAPI>>>
            {
                {APIProvider.VedAstro, GeoLocationToTimezone_Vedastro},
                {APIProvider.Azure, GeoLocationToTimezone_Azure},
                {APIProvider.Google, GeoLocationToTimezone_Google},
            };

            //start with empty as default if fail
            var timezoneStr = "";

            // Iterate over the list of functions
            foreach (var row in geoLocationProviders)
            {
                var provider = row.Value;
                var fullGeoRowData = (await provider(geoLocation, stdTimeAtLocation));
                timezoneStr = fullGeoRowData.MainRow.TimezoneText; //exp, +08:00

                // when new location not is cache, we add it
                // only add to cache if not empty and not VedAstro
                var isNotEmpty = !(string.IsNullOrEmpty(timezoneStr));
                var apiProvider = row.Key;
                var isNotVedAstro = apiProvider != APIProvider.VedAstro;
                if (isNotEmpty && isNotVedAstro)
                {
                    AddToTimezoneTable(fullGeoRowData.MainRow);
                    AddToTimezoneMetadataTable(fullGeoRowData.MetadataRow);
                }

                //once found, stop searching for location with APIs
                if (isNotEmpty) { break; }
            }

            //3 : SEND TO CALLER
            return timezoneStr;
        }




        //----------------------------------PRIVATE FUNCS-----------------------------

        /// <summary>
        /// Will add new cache to Geo Location Cache
        /// </summary>
        //private void AddToCache(GeoLocation parsedGeoLocation, string rowKeyData = "", string timezone = "", string source = "")
        //{

        //    //if cleaned name is same with user input name (RowKey), than remove cleaned name (save space)
        //    var cleanedName = CreateSearchableName(parsedGeoLocation.Name());
        //    cleanedName = cleanedName == rowKeyData ? "" : cleanedName;

        //    //NOTES
        //    //Azure Table Storage is designed for fast point
        //    //queries where the client knows the
        //    //Partition Key and Row Key

        //    //package the data
        //    GeoLocationCacheEntity customerEntity = new()
        //    {
        //        PartitionKey = parsedGeoLocation.Name(), //name given by Google API
        //        CleanedName = cleanedName, //used for fuzzy search on query side
        //        RowKey = rowKeyData, //row key data can be time or name inputed by caller
        //        Timezone = timezone,
        //        Latitude = parsedGeoLocation.Latitude(),
        //        Longitude = parsedGeoLocation.Longitude(),
        //        Source = source // used for identifying who made it, for validation checking
        //    };

        //    //creates record if no exist, update if already there
        //    tableClient.UpsertEntity(customerEntity);
        //}

        private void AddToTimezoneTable(GeoLocationTimezoneEntity newRow)
        {
            // If an identical entity exists, the call to
            // AddEntity would cause a duplicate entry error.
            // Therefore, before calling AddEntity,
            // ensure that the entity does not already exist
            try
            {
                timezoneTableClient.AddEntity(newRow);
            }
            catch (Exception e)
            {
                var errorMessage = $"Can't add duplicate GeoLocationTimezone row : PartKey:{newRow.PartitionKey}, RowKey:{newRow.RowKey}";

#if DEBUG
                Console.WriteLine(errorMessage);
#else
                LibLogger.Error(errorMessage);
#endif
                //this is critical and should not propagate!

                throw new Exception(errorMessage);
            }
        }

        private void AddToTimezoneMetadataTable(GeoLocationTimezoneMetadataEntity newRow)
        {
            // If an identical entity exists, the call to
            // AddEntity would cause a duplicate entry error.
            // Therefore, before calling AddEntity,
            // ensure that the entity does not already exist
            try
            {
                timezoneMetadataTableClient.AddEntity(newRow);
            }
            catch (Exception e)
            {
                //NOTE:
                //here we expect multiple adds of same metadata row,
                //so we do Add instead of Upsert to save money,
                //and if fail because can't add then proceed as normal
#if DEBUG
                Console.WriteLine("Metadata already exist...continuing");
#endif
            }

        }

        private void AddToIpAddressMetadataTable(IpAddressGeoLocationMetadataEntity newRow)
        {
            // If an identical entity exists, the call to
            // AddEntity would cause a duplicate entry error.
            // Therefore, before calling AddEntity,
            // ensure that the entity does not already exist
            try
            {
                ipAddressMetadataTableClient.AddEntity(newRow);
            }
            catch (Exception e)
            {
                //NOTE:
                //here we expect multiple adds of same metadata row,
                //so we do Add instead of Upsert to save money,
                //and if fail because can't add then proceed as normal
#if DEBUG
                Console.WriteLine("Metadata already exist...continuing");
#endif
            }

        }

        private void AddToAddressTable(AddressGeoLocationEntity newRow)
        {
            // If an identical entity exists, the call to
            // AddEntity would cause a duplicate entry error.
            // Therefore, before calling AddEntity,
            // ensure that the entity does not already exist
            try
            {
                addressTableClient.AddEntity(newRow);
            }
            catch (Exception e)
            {
                var errorMessage = $"Can't add duplicate AddressGeoLocation row : PartKey:{newRow.PartitionKey}, RowKey:{newRow.RowKey}";

#if DEBUG
                Console.WriteLine(errorMessage);
#else
                LibLogger.Error(errorMessage);
#endif
                //this is critical and should not propagate!

                throw new Exception(errorMessage);
            }
        }

        private void AddToCoordinatesTable(CoordinatesGeoLocationEntity newRow)
        {
            // If an identical entity exists, the call to
            // AddEntity would cause a duplicate entry error.
            // Therefore, before calling AddEntity,
            // ensure that the entity does not already exist
            try
            {
                coordinatesTableClient.AddEntity(newRow);
            }
            catch (Exception e)
            {
                var errorMessage = $"Can't add duplicate CoordinatesGeoLocation row : PartKey:{newRow.PartitionKey}, RowKey:{newRow.RowKey}";

#if DEBUG
                Console.WriteLine(errorMessage);
#else
                LibLogger.Error(errorMessage);
#endif
                //this is critical and should not propagate!

                throw new Exception(errorMessage);
            }
        }

        private void AddToIpAddressTable(IpAddressGeoLocationEntity newRow)
        {
            // If an identical entity exists, the call to
            // AddEntity would cause a duplicate entry error.
            // Therefore, before calling AddEntity,
            // ensure that the entity does not already exist
            try
            {
                ipAddressTableClient.AddEntity(newRow);
            }
            catch (Exception e)
            {
                var errorMessage = $"Can't add duplicate IpAddressGeoLocation row : PartKey:{newRow.PartitionKey}, RowKey:{newRow.RowKey}";

#if DEBUG
                Console.WriteLine(errorMessage);
#else
                LibLogger.Error(errorMessage);
#endif
                //this is critical and should not propagate!

                throw new Exception(errorMessage);
            }
        }




        //--------------- VEDASTRO -----------------
        //note async is needed to maintain interop

        private async Task<GeoLocationRawAPI> IpAddressToGeoLocation_VedAstro(string ipAddress)
        {
            //make a search for ip address stored under row key
            Expression<Func<IpAddressGeoLocationEntity, bool>> expression = call => call.PartitionKey == ipAddress;

            //execute search
            var recordFound = ipAddressTableClient.Query(expression).FirstOrDefault();

            //if old call found check if running else default false
            //NOTE : important return empty, because used to detect later if empty
            var foundRaw = recordFound ?? IpAddressGeoLocationEntity.Empty;

            //we don't supply metadata cause not needed, as separate query
            return new GeoLocationRawAPI(foundRaw, null);
        }

        private async Task<GeoLocationRawAPI> GeoLocationToTimezone_Vedastro(GeoLocation geoLocation, DateTimeOffset timeAtLocation)
        {

            //HANDLE LOCATIONS WHERE DST (DAYLIGHT SAVINGS)
            //time that is linked to timezone
            //NOTE :reduce accuracy to days so time is removed (this only checks, another writes)
            //      done to reduce cache clogging, so might miss offset by hours but not days
            //      !!DO NOT lower accuracy below time as needed for Western daylight saving changes!! 
            var roundedTime = new DateTimeOffset(timeAtLocation.Year, timeAtLocation.Month, timeAtLocation.Day, 0, 0, 0, timeAtLocation.Offset);

            //do search only with latitude, longitude and time
            Expression<Func<GeoLocationTimezoneEntity, bool>> expression = call => call.PartitionKey == geoLocation.GetPartitionKey() //lat & long 
                                                                                && call.RowKey == roundedTime.ToRowKey();

            var recordFound = timezoneTableClient.Query(expression).FirstOrDefault();

            //get timezone data out
            var foundRaw = recordFound ?? GeoLocationTimezoneEntity.Empty;

            //we don't supply metadata cause not needed, as separate query
            return new GeoLocationRawAPI(foundRaw, null);

        }

        /// <summary>
        /// Will return empty Geo Location if no cache
        /// NOTE: keep async for easy selection with other methods
        /// </summary>
        private async Task<GeoLocationRawAPI> AddressToGeoLocation_VedAstro(string userInputAddress)
        {
            //do direct search for address in name field
            Expression<Func<AddressGeoLocationEntity, bool>> expression = call => call.RowKey == userInputAddress;

            var recordFound = addressTableClient.Query(expression).FirstOrDefault();

            //if old call found check if running else default false
            //NOTE : important return empty, because used to detect later if empty
            var foundRaw = recordFound ?? AddressGeoLocationEntity.Empty;

            //we don't supply metadata cause not needed, as separate query
            return new GeoLocationRawAPI(foundRaw, null);

        }

        /// <summary>
        /// Will return empty Geo Location if no cache
        /// </summary>
        private async Task<GeoLocationRawAPI> CoordinatesToGeoLocation_Vedastro(double longitude, double latitude)
        {
            var linqEntities = coordinatesTableClient.Query<CoordinatesGeoLocationEntity>(
                    call => call.PartitionKey == latitude.ToString() && call.RowKey == longitude.ToString())
                .FirstOrDefault();

            //if old call found check if running else default false
            //NOTE : important return empty, because used to detect later if empty
            var foundRaw = linqEntities ?? CoordinatesGeoLocationEntity.Empty;

            //we don't supply metadata cause not needed, as separate query
            return new GeoLocationRawAPI(foundRaw, null);

        }



        //--------------- GOOGLE ------------------

        /// <summary>
        /// Will get longitude and latitude from IP using google API
        /// NOTE: The only place so far Google API outside VedAstro API
        /// </summary>
        private static async Task<GeoLocationRawAPI> IpAddressToGeoLocation_Google(string ipAddress)
        {
            throw new NotImplementedException();
            //TODO NOT FIXED, FOR NOW IP NOT INJECTED
            //return new WebResult<GeoLocation>(false, GeoLocation.Empty);
            //var apiKey = Secrets.GoogleAPIKey;
            //var url = $"https://www.googleapis.com/geolocation/v1/geolocate?key={apiKey}";
            //var resultString = await Tools.WriteServer<JObject, object>(HttpMethod.Post, url);

            ////get raw value 
            //var rawLat = resultString["location"]["lat"].Value<double>();
            //var rawLong = resultString["location"]["lng"].Value<double>();

            //var result = new GeoLocation("", rawLong, rawLat);

            //return new WebResult<GeoLocation>(true, result); ;
        }

        private static async Task<GeoLocationRawAPI> GeoLocationToTimezone_Google(GeoLocation geoLocation, DateTimeOffset timeAtLocation)
        {

            throw new NotImplementedException();

            //var returnResult = new WebResult<string>();

            ////use timestamp to account for historic timezone changes
            //var locationTimeUnix = timeAtLocation.ToUnixTimeSeconds();
            //var longitude = geoLocation.Longitude();
            //var latitude = geoLocation.Latitude();

            ////create the request url for Google API 
            //var apiKey = Secrets.GoogleAPIKey;
            //var url = string.Format($@"https://maps.googleapis.com/maps/api/timezone/xml?location={latitude},{longitude}&timestamp={locationTimeUnix}&key={apiKey}");

            ////get raw location data from GoogleAPI
            //var apiResult = await Tools.ReadFromServerXmlReply(url);

            ////if result from API is a failure then use system timezone
            ////this is clearly an error, as such log it
            //TimeSpan offsetMinutes;
            //if (apiResult.IsPass) //all well
            //{
            //    //get the raw data from google
            //    var timeZoneResponseXml = apiResult.Payload;

            //    //try parse Google API's payload
            //    var isParsed = Tools.TryParseGoogleTimeZoneResponse(timeZoneResponseXml, out offsetMinutes);
            //    if (!isParsed)
            //    {
            //        //mark as fail & use possibly inaccurate backup timezone (client browser's timezone)
            //        returnResult.IsPass = false;
            //        returnResult.Payload = "";
            //    }
            //    else
            //    {
            //        //convert to string exp: +08:00
            //        var parsedOffsetString = Tools.TimeSpanToUTCTimezoneString(offsetMinutes);

            //        //place data inside capsule
            //        returnResult.Payload = parsedOffsetString;
            //        returnResult.IsPass = true;

            //    }
            //}
            //else
            //{
            //    //mark as fail & use possibly inaccurate backup timezone (client browser's timezone)
            //    returnResult.IsPass = false;
            //    returnResult.Payload = "";
            //}

            //return returnResult.Payload;

        }

        /// <summary>
        /// Gets coordinates from Google API
        /// </summary>
        private static async Task<GeoLocationRawAPI> CoordinatesToGeoLocation_Google(double longitude, double latitude)
        {
            var apiKey = Secrets.GoogleAPIKey;
            var urlReverse = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude},{longitude}&key={apiKey}";

            //get location data from Azure Maps API
            var apiResult = await Tools.ReadFromServerJsonReply(urlReverse);

            //based on reply from API pack data for caller
            var returnResult = new WebResult<GeoLocationRawAPI>();
            if (apiResult.IsPass) // ALL WELL
            {
                // Parse Azure API's payload
                var outData = TryParseGoogleCoordinatesResponse(apiResult.Payload, longitude, latitude);
                bool isParsed = outData.IsParsed;
                if (isParsed)
                {
                    // Convert to string (example: +08:00)
                    returnResult.Payload = new GeoLocationRawAPI(outData.MainRow, null);
                    returnResult.IsPass = true;
                }
                else
                {
                    // Mark as fail & return empty for fail detection
                    returnResult.IsPass = false;
                    returnResult.Payload = new GeoLocationRawAPI(CoordinatesGeoLocationEntity.Empty, null);
                }
            }
            else // FAIL
            {
                // Mark as fail & return empty for fail detection
                returnResult.IsPass = false;
                returnResult.Payload = new GeoLocationRawAPI(CoordinatesGeoLocationEntity.Empty, null);
            }

            return returnResult;

        }

        private static async Task<GeoLocationRawAPI> AddressToGeoLocation_Google(string address)
        {
            throw new NotImplementedException();

            //if null or empty turn back as nothing
            //if (string.IsNullOrEmpty(address)) { return new WebResult<GeoLocation>(false, GeoLocation.Empty); }

            //create the request url for Google API
            var apiKey = Secrets.GoogleAPIKey;
            var url = $"https://maps.googleapis.com/maps/api/geocode/xml?key={apiKey}&address={Uri.EscapeDataString(address)}&sensor=false";

            //get location data from GoogleAPI
            var webResult = await Tools.ReadFromServerXmlReply(url);

            //if fail to make call, end here
            //if (!webResult.IsPass) { return new WebResult<GeoLocation>(false, GeoLocation.Empty); }

            //if success, get the reply data out
            var geocodeResponseXml = webResult.Payload;
            var resultXml = geocodeResponseXml.Element("result");
            var statusXml = geocodeResponseXml.Element("status");

#if DEBUG
            //DEBUG
            Console.WriteLine(geocodeResponseXml.ToString());
#endif

            //check the data, if location was NOT found by google API, end here
            var statusMsg = statusXml.Value;
            //if (statusXml == null || statusMsg == "ZERO_RESULTS" || statusMsg == "REQUEST_DENIED") { return new WebResult<GeoLocation>(false, GeoLocation.Empty); }

            //if success, extract out the longitude & latitude
            var locationElement = resultXml?.Element("geometry")?.Element("location");
            var lat = double.Parse(locationElement?.Element("lat")?.Value ?? "0");
            var lng = double.Parse(locationElement?.Element("lng")?.Value ?? "0");

            //round coordinates to 3 decimal places
            lat = Math.Round(lat, 3);
            lng = Math.Round(lng, 3);

            //get full name with country & state
            var fullName = resultXml?.Element("formatted_address")?.Value;

            //return to caller pass
            //return new WebResult<GeoLocation>(true, new GeoLocation(fullName, lng, lat));
        }



        //--------------- AZURE -----------------

        private static async Task<GeoLocationRawAPI> IpAddressToGeoLocation_Azure(string ipAddress)
        {
            throw new NotImplementedException();

            //TODO
//            return new WebResult<GeoLocation>(false, GeoLocation.Empty);

//            if (string.IsNullOrEmpty(ipAddress)) { return new WebResult<GeoLocation>(false, GeoLocation.Empty); }

//            var apiKey = Secrets.AzureMapsAPIKey;
//            //var url = $"https://atlas.microsoft.com/ip/reverse/json?api-version=1.0&subscription-key={apiKey}&ipAddress={ipAddress}";
//            //var apiKey = "<YOUR_API_KEY>";
//            var baseUrl = @"https://atlas.microsoft.com/ip/reverse/json?api-version=1.0&subscription-key=";
//            var regionSpecificEndpoint = "WestEurope"; // Change according to your subscription region
//            var url = $"{baseUrl}{apiKey}&ipAddress={Uri.EscapeDataString(ipAddress)}&region={regionSpecificEndpoint}";

//            var webResult = await VedAstro.Library.Tools.ReadFromServerJsonReply(url);

//            if (!webResult.IsPass) { return new WebResult<GeoLocation>(false, GeoLocation.Empty); }

//            var reverseIPResponseJson = webResult.Payload;

//#if DEBUG
//            Console.WriteLine(reverseIPResponseJson.ToString());
//#endif

//            //if (reverseIPResponseJson == null || !reverseIPResponseJson.ContainsKey("record")) { return new WebResult<GeoLocation>(false, GeoLocation.Empty); }

//            var record = reverseIPResponseJson["record"];

//            if (record == null || !(record is JArray array) || array.Count < 1 || !(array[0] is JObject firstRecord)) { return new WebResult<GeoLocation>(false, GeoLocation.Empty); }

//            JToken positionToken;
//            if (!firstRecord.TryGetValue("position", out positionToken)) { return new WebResult<GeoLocation>(false, GeoLocation.Empty); };

//            JObject positionObj = (JObject)positionToken;

//            double lat;
//            double lon;

//            if (!double.TryParse(positionObj["lat"].Value<string>(), NumberStyles.Float, CultureInfo.InvariantCulture, out lat)) { return new WebResult<GeoLocation>(false, GeoLocation.Empty); }
//            if (!double.TryParse(positionObj["lon"].Value<string>(), NumberStyles.Float, CultureInfo.InvariantCulture, out lon)) { return new WebResult<GeoLocation>(false, GeoLocation.Empty); }

//            lat = Math.Round(lat, 3);
//            lon = Math.Round(lon, 3);

//            string fullName = "";
//            if (firstRecord.TryGetValue("address", out JToken addressTok))
//            {
//                JObject addressObj = (JObject)addressTok;
//                if (addressObj.TryGetValue("freeformAddress", StringComparison.OrdinalIgnoreCase, out JToken addrTextTok))
//                {
//                    fullName = addrTextTok.Value<string>();
//                }
//            }

//            if (string.IsNullOrWhiteSpace(fullName))
//            {
//                if (firstRecord.TryGetValue("locality", out JToken localityTok))
//                {
//                    fullName = localityTok.Value<string>();
//                }
//            }

//            if (string.IsNullOrWhiteSpace(fullName))
//            {
//                if (firstRecord.TryGetValue("administrativeArea5", out JToken area5Tok))
//                {
//                    fullName = area5Tok.Value<string>();
//                }
//            }

//            if (string.IsNullOrWhiteSpace(fullName))
//            {
//                if (firstRecord.TryGetValue("administrativeArea3", out JToken area3Tok))
//                {
//                    fullName = area3Tok.Value<string>();
//                }
//            }

//            if (string.IsNullOrWhiteSpace(fullName))
//            {
//                if (firstRecord.TryGetValue("countrySubdivision", out JToken subDivTok))
//                {
//                    fullName = subDivTok.Value<string>();
//                }
//            }

//            if (string.IsNullOrWhiteSpace(fullName))
//            {
//                if (firstRecord.TryGetValue("countryCode", out JToken countryCodeTok))
//                {
//                    fullName = countryCodeTok.Value<string>();
//                }
//            }

//            if (string.IsNullOrWhiteSpace(fullName))
//            {
//                return new WebResult<GeoLocation>(false, GeoLocation.Empty);
//            }

//            return new WebResult<GeoLocation>(true, new GeoLocation(fullName, lon, lat));
        }

        private static async Task<GeoLocationRawAPI> AddressToGeoLocation_Azure(string userInputAddress)
        {
            var returnResult = new WebResult<GeoLocationRawAPI>();

            //create the request url for Azure Maps API
            var apiKey = Secrets.AzureMapsAPIKey;
            var url = $"https://atlas.microsoft.com/search/address/json?api-version=1.0&subscription-key={apiKey}&query={Uri.EscapeDataString(userInputAddress)}";

            //get location data from Azure Maps API
            var apiResult = await Tools.ReadFromServerJsonReply(url);

            // If result from API is a failure, use the system time zone as fallback
            if (apiResult.IsPass) // All well
            {
                // Parse Azure API's payload
                var outData = TryParseAzureAddressResponse(apiResult.Payload, userInputAddress);
                bool isParsed = outData.IsParsed;
                if (isParsed)
                {
                    // Convert to string (example: +08:00)
                    returnResult.Payload = new GeoLocationRawAPI(outData.MainRow, null);
                    returnResult.IsPass = true;
                }
                else
                {
                    // Mark as fail & return empty for fail detection
                    returnResult.IsPass = false;
                    returnResult.Payload = new GeoLocationRawAPI(AddressGeoLocationEntity.Empty, null);
                }
            }
            else
            {
                // Mark as fail & return empty for fail detection
                returnResult.IsPass = false;
                returnResult.Payload = new GeoLocationRawAPI(GeoLocationTimezoneEntity.Empty, GeoLocationTimezoneMetadataEntity.Empty);
            }

            return returnResult;

        }

        private static async Task<GeoLocationRawAPI> GeoLocationToTimezone_Azure(GeoLocation geoLocation, DateTimeOffset timeAtLocation)
        {
            var returnResult = new WebResult<GeoLocationRawAPI>();

            // Convert the DateTimeOffset to ISO 8601 format ("yyyy-MM-ddTHH:mm:ssZ")
            var locationTimeIso8601 = timeAtLocation.ToString("O", System.Globalization.CultureInfo.InvariantCulture);

            // Create the request URL for Azure Maps API
            var apiKey = Secrets.AzureMapsAPIKey;
            var url = $@"https://atlas.microsoft.com/timezone/byCoordinates/json?api-version=1.0&subscription-key={apiKey}&query={geoLocation.Latitude()},{geoLocation.Longitude()}&timestamp={Uri.EscapeDataString(locationTimeIso8601)}";

            // Get raw location data from Azure Maps API
            var apiResult = await Tools.ReadFromServerJsonReply(url);

            // If result from API is a failure, use the system time zone as fallback
            if (apiResult.IsPass) // All well
            {
                // Parse Azure API's payload
                var outData = TryParseAzureTimeZoneResponse(timeAtLocation, geoLocation, apiResult.Payload);
                bool isParsed = outData.IsParsed;
                if (isParsed)
                {
                    // Convert to string (example: +08:00)
                    returnResult.Payload = new GeoLocationRawAPI(outData.MainRow, outData.MetadataRow);
                    returnResult.IsPass = true;
                }
                else
                {
                    // Mark as fail & return empty for fail detection
                    returnResult.IsPass = false;
                    returnResult.Payload = new GeoLocationRawAPI(GeoLocationTimezoneEntity.Empty, GeoLocationTimezoneMetadataEntity.Empty);
                }
            }
            else
            {
                // Mark as fail & return empty for fail detection
                returnResult.IsPass = false;
                returnResult.Payload = new GeoLocationRawAPI(GeoLocationTimezoneEntity.Empty, GeoLocationTimezoneMetadataEntity.Empty);
            }

            return returnResult;
        }

        private static dynamic TryParseAzureTimeZoneResponse(DateTimeOffset timeAtLocation, GeoLocation geoLocation, JToken timeZoneResponseJson)
        {
            try
            {
                //STEP 1: validate the data
                var timeZonesArray = timeZoneResponseJson["TimeZones"] as JArray;
                if (timeZonesArray == null || !timeZonesArray.HasValues)
                    throw new ArgumentException($"Invalid or missing 'TimeZones' property in Azure timezone response.");

                var firstTimeZoneObject = timeZonesArray[0] as JObject;
                if (firstTimeZoneObject == null)
                    throw new ArgumentException($"Invalid or missing timezone object in Azure timezone response.");

                var referenceTimeObject = firstTimeZoneObject["ReferenceTime"] as JObject;
                if (referenceTimeObject == null)
                    throw new ArgumentException($"Invalid or missing 'ReferenceTime' object in Azure timezone response.");

                //STEP 2: get data out 
                var standardOffsetString = referenceTimeObject["StandardOffset"].Value<string>();
                var stdOffsetMinutes = TimeSpan.Parse(standardOffsetString);

                //difference in daylight savings
                //when no daylight saving will return 0
                var daylightSavingsString = referenceTimeObject["DaylightSavings"].Value<string>();
                var daylightSavingsOffsetMinutes = TimeSpan.Parse(daylightSavingsString);

                //add standard never changing offset to daylight savings to get final accurate timezone
                var finalOffsetMinutes = stdOffsetMinutes + daylightSavingsOffsetMinutes;
                var timezoneText = VedAstro.Library.Tools.TimeSpanToUTCTimezoneString(finalOffsetMinutes);

                //STEP 3: CONVERT FORMAT (AZURE -> VEDASTRO CACHE DB)
                //NOTE: from Azure's response, 2 table row data types is created,
                //      timezone data and metadata (stored separately to save duplicate writes)

                //# TYPE 1 : ONLY TIMEZONE BY COORDINATES
                var timezoneRow = new GeoLocationTimezoneEntity();
                timezoneRow.PartitionKey = geoLocation.GetPartitionKey();

                //NOTE :reduce accuracy to days so time is removed (this only writes, another checks)
                //      done to reduce cache clogging, so might miss offset by hours but not days
                //      !!DO NOT lower accuracy below time as needed for Western daylight saving changes!! 
                DateTimeOffset roundedTime = new DateTimeOffset(timeAtLocation.Year, timeAtLocation.Month, timeAtLocation.Day, 0, 0, 0, timeAtLocation.Offset);
                timezoneRow.TimezoneText = timezoneText;
                timezoneRow.RowKey = roundedTime.ToRowKey();

                //# TYPE 2 : METADATA FOR TIMEZONE
                //fill the timezone metadata row details
                var timezoneMetadataRow = new GeoLocationTimezoneMetadataEntity();
                timezoneMetadataRow.StandardOffset = Tools.TimeSpanToUTCTimezoneString(stdOffsetMinutes);
                timezoneMetadataRow.DaylightSavings = Tools.TimeSpanToUTCTimezoneString(daylightSavingsOffsetMinutes);
                timezoneMetadataRow.Tag = (referenceTimeObject["Tag"] ?? "").Value<string>() ?? string.Empty;
                timezoneMetadataRow.Standard_Name = (firstTimeZoneObject["Names"]?["Standard"] ?? "").Value<string>() ?? string.Empty;
                timezoneMetadataRow.Daylight_Name = (firstTimeZoneObject["Names"]?["Daylight"] ?? "").Value<string>() ?? string.Empty;
                timezoneMetadataRow.ISO_Name = (firstTimeZoneObject["Id"] ?? "").Value<string>() ?? string.Empty;
                timezoneMetadataRow.RowKey = "0";//DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(8)).ToRowKey(); //to know when was created

                //NOTE: linking is done last, because hash is based on data, and it needs to be filled 1st
                timezoneMetadataRow.PartitionKey = timezoneMetadataRow.CalculateCombinedHash();
                timezoneRow.MetadataHash = timezoneMetadataRow.PartitionKey; //link the timezone to it's metadata

                return new { IsParsed = true, MainRow = timezoneRow, MetadataRow = timezoneMetadataRow };
            }
            catch
            {
                //if fail return empty and fail
                return new { IsParsed = false, MainRow = GeoLocationTimezoneEntity.Empty, MetadataRow = GeoLocationTimezoneMetadataEntity.Empty };
            }
        }

        private static dynamic TryParseAzureAddressResponse(JToken geocodeResponseJson, string userInputAddress)
        {
            try
            {
                var rawAzureReply = geocodeResponseJson["results"][0];

                //check the data, if location was NOT found by Azure Maps API, end here
                if (rawAzureReply == null || rawAzureReply["type"].Value<string>() != "Geography") { return null; }

                //if success, extract out the longitude & latitude
                var locationElement = rawAzureReply["position"];
                var lat = double.Parse(locationElement["lat"].Value<string>() ?? "0");
                var lng = double.Parse(locationElement["lon"].Value<string>() ?? "0");

                //round coordinates to 3 decimal places
                lat = Math.Round(lat, 3);
                lng = Math.Round(lng, 3);

                //get full name with country & state
                var freeformAddress = rawAzureReply["address"]["freeformAddress"].Value<string>();
                var country = rawAzureReply["address"]["country"].Value<string>();
                var fullName = $"{freeformAddress}, {country}";


                //# MAIN ROW
                var mainRow = new AddressGeoLocationEntity();
                mainRow.Latitude = lat;
                mainRow.Longitude = lng;
                mainRow.PartitionKey = fullName;
                mainRow.RowKey = Tools.CleanText(userInputAddress); //todo verify clean procedure


                return new { IsParsed = true, MainRow = mainRow };

            }
            catch
            {
                //if fail return empty and fail
                return new { IsParsed = false, TimezoneRow = AddressGeoLocationEntity.Empty };
            }
        }

        private static dynamic TryParseGoogleCoordinatesResponse(JToken geocodeResponseJson, double longitude, double latitude)
        {
            try
            {
                //get address data out
                //NOTE: multiple locations that match are given, only 1 first is taken
                var resultsJson = geocodeResponseJson["results"][0];
                var locationNameLong = resultsJson["formatted_address"].Value<string>();
                var splitted = locationNameLong.Split(',');

                //NOTE: front part sometimes contain, street address, not needed
                //keep only the last parts, country, state... EXP : Ipoh, Perak, Malaysia
                var fullName = $"{splitted[splitted.Length - 3]},{splitted[splitted.Length - 2]},{splitted[splitted.Length - 1]}";


                //# MAIN ROW
                var mainRow = new CoordinatesGeoLocationEntity();
                mainRow.PartitionKey = latitude.ToString();
                mainRow.RowKey = longitude.ToString();
                mainRow.Name = fullName.Trim(); //remove leading & trailing white space if any

                return new { IsParsed = true, MainRow = mainRow};

            }
            catch
            {
                //if fail return empty and fail
                return new { IsParsed = false, MainRow = CoordinatesGeoLocationEntity.Empty };
            }
        }




        //--------------- IP DATA -----------------


        private static async Task<GeoLocationRawAPI> IpAddressToGeoLocation_IpData(string ipAddress)
        {
            const string baseUrl = "https://api.ipdata.co";
            var apiKey = Secrets.IpDataAPIKey;
            var requestUri = $"{baseUrl}/{ipAddress}?api-key={apiKey}";

            //get location data from Azure Maps API
            var apiResult = await Tools.ReadFromServerJsonReply(requestUri);

            //based on reply from API pack data for caller
            var returnResult = new WebResult<GeoLocationRawAPI>();
            if (apiResult.IsPass) // ALL WELL
            {
                // Parse Azure API's payload
                var outData = TryParseIpDataCoordinatesResponse(apiResult.Payload, ipAddress);
                bool isParsed = outData.IsParsed;
                if (isParsed)
                {
                    // Convert to string (example: +08:00)
                    returnResult.Payload = new GeoLocationRawAPI(outData.MainRow, outData.MetadataRow);
                    returnResult.IsPass = true;
                }
                else
                {
                    // Mark as fail & return empty for fail detection
                    returnResult.IsPass = false;
                    returnResult.Payload = new GeoLocationRawAPI(IpAddressGeoLocationEntity.Empty, IpAddressGeoLocationMetadataEntity.Empty);
                }
            }
            else // FAIL
            {
                // Mark as fail & return empty for fail detection
                returnResult.IsPass = false;
                returnResult.Payload = new GeoLocationRawAPI(IpAddressGeoLocationEntity.Empty, IpAddressGeoLocationMetadataEntity.Empty);
            }

            return returnResult;

        }

        private static dynamic TryParseIpDataCoordinatesResponse(JToken geocodeResponseJson, string ipAddress)
        {
            try
            {

                //if success, extract out the longitude & latitude
                var latitude = double.Parse(geocodeResponseJson?["latitude"]?.Value<string>() ?? "0");
                var longitude = double.Parse(geocodeResponseJson?["longitude"]?.Value<string>() ?? "0");

                //round coordinates to 3 decimal places
                latitude = Math.Round(latitude, 3);
                longitude = Math.Round(longitude, 3);

                //get full name with country & state
                var region = geocodeResponseJson["region"].Value<string>();
                var country = geocodeResponseJson["country_name"].Value<string>();
                var fullName = $"{region}, {country}";

                //meta data
                var asn_name = geocodeResponseJson["asn"]["name"].Value<string>();
                var timezone_name = geocodeResponseJson["time_zone"]["name"].Value<string>();
                var timezone_offset = geocodeResponseJson["time_zone"]["offset"].Value<string>();
                var is_proxy = geocodeResponseJson["threat"]["is_proxy"].Value<string>();
                var is_datacenter = geocodeResponseJson["threat"]["is_datacenter"].Value<string>();
                var is_anonymous = geocodeResponseJson["threat"]["is_anonymous"].Value<string>();
                var is_known_attacker = geocodeResponseJson["threat"]["is_known_attacker"].Value<string>();
                var is_known_abuser = geocodeResponseJson["threat"]["is_known_abuser"].Value<string>();
                var is_threat = geocodeResponseJson["threat"]["is_threat"].Value<string>();
                var is_bogon = geocodeResponseJson["threat"]["is_bogon"].Value<string>();


                //# MAIN ROW
                var mainRow = new IpAddressGeoLocationEntity();
                mainRow.PartitionKey = ipAddress;
                mainRow.RowKey = "";
                mainRow.Latitude = latitude;
                mainRow.Longitude = longitude;
                mainRow.LocationName = fullName.Trim(); //remove leading & trailing white space if any


                //# METADATA
                //fill the metadata row details
                var metadataRow = new IpAddressGeoLocationMetadataEntity();
                metadataRow.AsnName = asn_name;
                metadataRow.TimezoneName = timezone_name;
                metadataRow.TimezoneOffset = timezone_offset;
                metadataRow.IsProxy = is_proxy;
                metadataRow.IsDatacenter = is_datacenter;
                metadataRow.IsAnonymous = is_anonymous;
                metadataRow.IsKnownAttacker = is_known_attacker;
                metadataRow.IsKnownAbuser = is_known_abuser;
                metadataRow.IsThreat = is_threat;
                metadataRow.IsBogon = is_bogon;

                metadataRow.RowKey = "0";//DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(8)).ToRowKey(); //to know when was created

                //NOTE: linking is done last, because hash is based on data, and it needs to be filled 1st
                metadataRow.PartitionKey = metadataRow.CalculateCombinedHash();
                mainRow.MetadataHash = metadataRow.PartitionKey; //link the main row to it's metadata

                return new { IsParsed = true, MainRow = mainRow, MetadataRow = metadataRow };

            }
            catch
            {
                //if fail return empty and fail
                return new { IsParsed = false, MainRow = IpAddressGeoLocationEntity.Empty, MetadataRow = IpAddressGeoLocationMetadataEntity.Empty };
            }
        }

    }
}
