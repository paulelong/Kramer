using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CloudStorage;

namespace MyMixes
{
    public class LocationMapping
    {
        public string path;
        public CloudStorage.CloudProviders provider;
    }

    class MixLocation
    {
        private static List<MixLocation> mixLocations;
        private string path;
        private ICloudStore cloudStore;

        public MixLocation(string path, ICloudStore cloudStore)
        {
            this.path = path;
            this.cloudStore = cloudStore;
        }

        //public async static Task<List<MixLocation>> GetMixLocationsAsync()
        //{
        //    if(mixLocations == null)
        //    {
        //        mixLocations = new List<MixLocation>();
        //        foreach(LocationMapping ml in PersistentData.LocationMappings)
        //        {
        //            mixLocations.Add(new MixLocation(ml.path, await ProviderInfo.GetCloudProviderAsync(ml.provider)));
        //        }
        //        // populate from persisted data
        //    }

        //    return mixLocations;
        //}

        public string RootPath { get; set; }

        public ProviderInfo Pi { get; set; }

        public async Task AddAsync(string rootPath, ProviderInfo _pi)
        {
           // mixLocations.Add(new MixLocation(rootPath, await _pi.GetCloudProviderAsync()));
        }

        public static void Load()
        {
            PersistentData.LoadProjectMappings();
        }

        //public static void Save()
        //{
        //    PersistentData.SaveProjectMappings();
        //}
    }
}
