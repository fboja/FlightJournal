﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FlightLog.Controllers
{
    using System.Data.Entity;
    using System.Web.Helpers;

    using FlightLog.Models;
    using FlightLog.ViewModels.Report;

    public class ReportController : Controller
    {
        private FlightContext db = new FlightContext();

        // GET: /Report/
        public ActionResult Index(DateTime? date)
        {
            // URL information can be reviewed using RouteData.Values["date"]
            var raw = RouteData.Values["date"];
            int year = 0;

            // YEAR Statistics
            if (!date.HasValue && raw != null)
            {
                if (int.TryParse(raw.ToString(), out year))
                {
                    if ((DateTime.Now.Year >= year) && (year > 1990))
                    {
                        var rptYear = new ReportViewModel();
                        rptYear.Date = new DateTime(year, 1, 1);
                        rptYear.Flights = this.db.Flights.Where(f => f.Date.Year == rptYear.Date.Year).OrderBy(o => o.Departure);
                        return this.View("year", rptYear);
                    }
                }
            }

            // MONTH Statistics
            if (raw != null && raw.ToString().Length < 9 && raw.ToString().StartsWith("20"))
            {
                var rptMonth = new ReportViewModel();
                if (date.HasValue)
                {
                    rptMonth.Date = date.Value;
                }
                else
                {
                    throw new ArgumentException(string.Format("Invalid date input in url: {0}", raw));
                }

                rptMonth.Flights = this.db.Flights.Where(f => f.Date.Month == rptMonth.Date.Month && f.Date.Year == rptMonth.Date.Year).Include("Betaler").OrderBy(o => o.Departure);

                return this.View("month", rptMonth);
            }

            var rpt = new ReportViewModel();
            rpt.AvailableDates = this.AvailableDates();
            if (rpt.AvailableDates.Count > 0 && !date.HasValue)
            {
                rpt.Date = rpt.AvailableDates.Max(d => d.Key);
            }
            else if (date.HasValue)
            {
                rpt.Date = date.Value;
            }
            else
            {
                rpt.Date = DateTime.Today;
            }

            rpt.Flights = this.db.Flights.Where(f => f.Date == rpt.Date).Include("Betaler").OrderBy(o => o.Departure);

            return this.View(rpt);
        }

        public FileContentResult Export(int year, int? month)
        {
            if (month.HasValue)
            {
                var flights = this.db.Flights.Where(f => f.Date.Month == month.Value && f.Date.Year == year).OrderBy(o => o.Departure);
                var csv = Enumerable.Aggregate(flights, this.SafeCSVParser(Flight.CsvHeaders), (current, flight) => current + this.SafeCSVParser(flight.ToCsvString()));
                return File(new System.Text.UTF8Encoding().GetBytes(csv), "text/csv", "Startlister-" + year + "-" + month + ".csv");
            }
            else
            {
                var flights = this.db.Flights.Where(f => f.Date.Year == year).OrderBy(o => o.Departure);
                var csv = Enumerable.Aggregate(flights, this.SafeCSVParser(Flight.CsvHeaders), (current, flight) => current + this.SafeCSVParser(flight.ToCsvString()));
                return File(new System.Text.UTF8Encoding().GetBytes(csv), "text/csv", "Startlister-" + year + ".csv");
            }
        }

        private string SafeCSVParser(string input)
        {
            // HACK: Fix for Encoding Issue with Danish letters and not wanting to use a component for creating the csv.
            var csv = input.Replace("æ", "ae");
            csv = csv.Replace("Æ", "AE");
            csv = csv.Replace("ø", "oe");
            csv = csv.Replace("Ø", "OE");
            csv = csv.Replace("å", "aa");
            csv = csv.Replace("Å", "AA");
            csv = csv.Replace("é", "e");
            return csv;
        }

        public Dictionary<DateTime, int> AvailableDates()
        {
            var availableDates = this.db.Flights.GroupBy(p => p.Date).Select(
                g => new { Date = g.Key, Flights = this.db.Flights.Where(d => d.Date == g.Key) });

            return availableDates.Select(d => new { d.Date, Hours = d.Flights.Count() }).ToDictionary(
                x => x.Date, x => x.Hours);
        }
    }
}
