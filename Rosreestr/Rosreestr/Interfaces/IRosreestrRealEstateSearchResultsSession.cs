using Shared.Classes;
using System.Collections.Generic;

namespace Rosreestr.Sessions
{
    public interface IRosreestrRealEstateSearchResultsSession
    {
        IList<AddressSearchInfo> Addresses { get; }

        bool NotFound { get; }

        IRosreestrOrderFormSession OpenOrderForm(int addressIndex, bool withCaptcha, bool isanul = false);

        IRosreestrRealEstateSearchSession ChangeSearchParameters();
    }
}
