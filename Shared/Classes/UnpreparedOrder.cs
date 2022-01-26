using System;

namespace Shared.Classes
{
    public class UnpreparedOrder : OrderBase
    {
        public int ID_Request { get; set; }

        public string District { get; set; }

        public string City { get; set; }

        public string Town { get; set; }

        public string Street { get; set; }

        public string Home { get; set; }

        public string Corp { get; set; }

        public string Flat { get; set; }

        public string Square { get; set; }

        public DateTime RecivedAt { get; set; }

        public string Address
        {
            get
            {
                string Address = string.Empty;

                if (!string.IsNullOrEmpty(City))
                {
                    Address += $"г.{City}";

                    if (!string.IsNullOrEmpty(Street))
                        Address += $", ул.{Street}";

                    if (!string.IsNullOrEmpty(Home))
                        Address += $", д.{Home}";

                    if (!string.IsNullOrEmpty(Corp))
                        Address += $", корп.{Corp}";

                    if (!string.IsNullOrEmpty(Flat))
                        Address += $", кв.{Flat}";
                }
                else if (!string.IsNullOrEmpty(District) || !string.IsNullOrEmpty(Town))
                {
                    if (!string.IsNullOrEmpty(District))
                        Address += $"р.{District}";

                    if (!string.IsNullOrEmpty(Town))
                        Address += $", {Town}";

                    if (!string.IsNullOrEmpty(Street))
                        Address += $", ул.{Street}";

                    if (!string.IsNullOrEmpty(Home))
                        Address += $", д.{Home}";

                    if (!string.IsNullOrEmpty(Corp))
                        Address += $", корп.{Corp}";

                    if (!string.IsNullOrEmpty(Flat))
                        Address += $", кв.{Flat}";
                }

                return Address;
            }
        }
    }
}