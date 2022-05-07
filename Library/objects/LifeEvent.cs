﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Genso.Astrology.Library
{
    /// <summary>
    /// Simple data type to hold info on life events of a person
    /// This event is to mark important moments in a persons life
    /// This is used later by calculators like Dasa to show against astrological predictions
    /// </summary>
    public class LifeEvent : IToXml
    {


        //░█▀▀▄ ─█▀▀█ ▀▀█▀▀ ─█▀▀█ 
        //░█─░█ ░█▄▄█ ─░█── ░█▄▄█ 
        //░█▄▄▀ ░█─░█ ─░█── ░█─░█

        /// <summary>
        /// Custom name of the event
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Must follow standard time formatting 
        /// </summary>
        public string StartTime { get; set; }

        /// <summary>
        /// Must follow standard time formatting 
        /// </summary>
        public string EndTime { get; set; }

        /// <summary>
        /// must be Good, Neutral or Bad only
        /// </summary>
        public string Nature { get; set; }





        //░█▀▄▀█ ░█▀▀▀ ▀▀█▀▀ ░█─░█ ░█▀▀▀█ ░█▀▀▄ ░█▀▀▀█ 
        //░█░█░█ ░█▀▀▀ ─░█── ░█▀▀█ ░█──░█ ░█─░█ ─▀▀▀▄▄ 
        //░█──░█ ░█▄▄▄ ─░█── ░█─░█ ░█▄▄▄█ ░█▄▄▀ ░█▄▄▄█


        public XElement ToXml()
        {
            var lifeEventXml = new XElement("LifeEvent");
            var nameXml = new XElement("Name", this.Name);
            var startTimeXml = new XElement("StartTime", this.StartTime);
            var endTimeXml = new XElement("EndTime", this.EndTime);
            var natureXml = new XElement("Nature", this.Nature);

            lifeEventXml.Add(nameXml, startTimeXml, endTimeXml, natureXml);

            return lifeEventXml;
        }

        /// <summary>
        /// The root element is expected to be LifeEvent
        /// Note: Special method done to implement IToXml
        /// </summary>
        public dynamic FromXml<T>(XElement xml) where T : IToXml => FromXml(xml);

        /// <summary>
        /// The root element is expected to be Person
        /// </summary>
        public static LifeEvent FromXml(XElement lifeEventXml)
        {
            var lifeEventParsed = new LifeEvent
            {
                Name = lifeEventXml.Element("Name").Value,
                StartTime = lifeEventXml.Element("StartTime").Value,
                EndTime = lifeEventXml.Element("EndTime").Value,
                Nature = lifeEventXml.Element("Nature").Value,
            };

            return lifeEventParsed;

        }





        //░█▀▀▀█ ░█──░█ ░█▀▀▀ ░█▀▀█ ░█▀▀█ ▀█▀ ░█▀▀▄ ░█▀▀▀ 　 ░█▀▄▀█ ░█▀▀▀ ▀▀█▀▀ ░█─░█ ░█▀▀▀█ ░█▀▀▄ ░█▀▀▀█ 
        //░█──░█ ─░█░█─ ░█▀▀▀ ░█▄▄▀ ░█▄▄▀ ░█─ ░█─░█ ░█▀▀▀ 　 ░█░█░█ ░█▀▀▀ ─░█── ░█▀▀█ ░█──░█ ░█─░█ ─▀▀▀▄▄ 
        //░█▄▄▄█ ──▀▄▀─ ░█▄▄▄ ░█─░█ ░█─░█ ▄█▄ ░█▄▄▀ ░█▄▄▄ 　 ░█──░█ ░█▄▄▄ ─░█── ░█─░█ ░█▄▄▄█ ░█▄▄▀ ░█▄▄▄█


        public override bool Equals(object value)
        {

            if (value.GetType() == typeof(LifeEvent))
            {
                //cast to type
                var parsedValue = (LifeEvent)value;

                //check equality
                bool returnValue = (this.GetHashCode() == parsedValue.GetHashCode());

                return returnValue;
            }
            else
            {
                //return false if value is null
                return false;
            }
        }

        public override string ToString()
        {
            //prepare string
            var returnString = $"{this.Name} - {this.Nature} - {this.StartTime} - {this.EndTime}";

            //return string to caller
            return returnString;
        }

        /// <summary>
        /// Name & Birth Time are used to generate Hash
        /// </summary>
        public override int GetHashCode()
        {
            //get hash of all the fields & combine them
            var hash1 = Tools.GetHashCode(this.Name);
            var hash2 = Tools.GetHashCode(this.StartTime);
            var hash3 = Tools.GetHashCode(this.EndTime);
            var hash4 = Tools.GetHashCode(this.Nature);

            //take out negative before returning
            return Math.Abs(hash1 + hash2 + hash3 + hash4);
        }



    }
}
