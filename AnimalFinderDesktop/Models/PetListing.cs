using System;

namespace AnimalFinderDesktop.Models
{
    public class PetListing
    {
        public string id { get; set; }
        public string user_id { get; set; }
        public string listing_type { get; set; }
        public string pet_name { get; set; }
        public string species { get; set; }
        public string breed { get; set; }
        public string color { get; set; }
        public int? age { get; set; }
        public string gender { get; set; }
        public string size { get; set; }
        public string photo_url { get; set; }
        public string location { get; set; }
        public double? latitude { get; set; }
        public double? longitude { get; set; }
        public string description { get; set; }
        public string contact { get; set; }
        public string contact_phone { get; set; }
        public DateTime created_at { get; set; }
        public string status { get; set; }
        public string microchip { get; set; }
        public string special_marks { get; set; }
    }
}