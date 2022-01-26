namespace Rosreestr.Sessions
{
    /// <summary>
    /// Форма поиска объектов недвижимости
    /// </summary>
    public interface IRosreestrRealEstateSearchSession
    {
        IRosreestrRealEstateSearchResultsSession SearchAddress(
            string region, string cadastralNumber);

        IRosreestrRealEstateSearchResultsSession SearchAddress(
                string region, string district, string city, string street, string home, string corp, string flat);
    }
}
