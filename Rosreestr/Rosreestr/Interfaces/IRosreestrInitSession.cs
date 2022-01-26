namespace Rosreestr.Sessions
{
    public interface IRosreestrInitSession
    {
        IRosreestrRealEstateSearchSession OpenRealEstateSearchForm();

        IRosreestrNumberSearchSession OpenNumberSearchFrom();
    }
}
