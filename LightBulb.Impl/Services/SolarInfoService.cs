﻿using System;
using LightBulb.Models;

namespace LightBulb.Services
{
    public class SolarInfoService : ISolarInfoService
    {
        // Original code credit:
        // https://github.com/ceeK/Solar/blob/9d8ed80a3977c97d7a2014ef28b129ec80c52a70/Solar/Solar.swift
        // Copyright (c) 2016 Chris Howell (MIT License)

        private enum ExpectedSolarState
        {
            Sunrise,
            Sunset
        }

        private double DegreesToRadians(double degree) => degree * (Math.PI / 180);

        private double RadiansToDegrees(double radians) => radians * 180 / Math.PI;

        private DateTime Calculate(double lat, double lng, DateTime date, ExpectedSolarState state)
        {
            // Get day of year
            var day = date.DayOfYear;

            // Convert longitude to hour value and calculate an approximate time
            var lngHours = lng / 15;
            var timeApproxHours = state == ExpectedSolarState.Sunrise ? 6 : 18;
            var timeApproxDays = day + (timeApproxHours - lngHours) / 24;

            // Calculate the Sun's mean anomaly
            var sunMeanAnomaly = 0.9856 * timeApproxDays - 3.289;

            // Calculate the Sun's true longitude
            var sunLng = sunMeanAnomaly + 282.634 +
                         1.916 * Math.Sin(DegreesToRadians(sunMeanAnomaly)) +
                         0.020 * Math.Sin(2 * DegreesToRadians(sunMeanAnomaly));
            sunLng = sunLng % 360; // wrap [0;360)

            // Calculate the Sun's right ascension
            var sunRightAsc = RadiansToDegrees(Math.Atan(0.91764 * Math.Tan(DegreesToRadians(sunLng))));
            sunRightAsc = sunRightAsc % 360; // wrap [0;360)

            // Right ascension value needs to be in the same quadrant as true longitude
            var sunLngQuad = Math.Floor(sunLng / 90) * 90;
            var sunRightAscQuad = Math.Floor(sunRightAsc / 90) * 90;
            var sunRightAscHours = sunRightAsc + (sunLngQuad - sunRightAscQuad);
            sunRightAscHours = sunRightAscHours / 15;

            // Calculate Sun's declination
            var sinDec = 0.39782 * Math.Sin(DegreesToRadians(sunLng));
            var cosDec = Math.Cos(Math.Asin(sinDec));

            // Calculate the Sun's local hour angle
            const double zenith = 90.83; // official sunrise/sunset
            var sunLocalHoursCos = (Math.Cos(DegreesToRadians(zenith)) - sinDec * Math.Sin(DegreesToRadians(lat))) /
                                  (cosDec * Math.Cos(DegreesToRadians(lat)));
            var sunLocalHours = state == ExpectedSolarState.Sunrise
                ? 360 - RadiansToDegrees(Math.Acos(sunLocalHoursCos))
                : RadiansToDegrees(Math.Acos(sunLocalHoursCos));
            sunLocalHours = sunLocalHours / 15;

            // Calculate local mean time
            var meanTime = sunLocalHours + sunRightAscHours - 0.06571 * timeApproxDays - 6.622;

            return date.Date.AddHours(meanTime);
        }

        public SolarInfo Get(double latitude, double longitude, DateTime date)
        {
            var sunrise = Calculate(latitude, longitude, date, ExpectedSolarState.Sunrise);
            var sunset = Calculate(latitude, longitude, date, ExpectedSolarState.Sunset);

            return new SolarInfo(sunrise.TimeOfDay, sunset.TimeOfDay);
        }
    }
}