using Simpleform.buidhouse.models;
using Simpleform.buidhouse.utils;

namespace Simpleform.buidhouse.dtos;

public record GridSectionConfig
{
    public double WidthHouse {get; init;}
    public double LengthHouse {get; init;}

    public double gridLineExtension {get; init;}

    public GridConfig ConvertToGridConfig()
    {
        return new GridConfig(RevitUtil.ConvertToFeet(WidthHouse), RevitUtil.ConvertToFeet(LengthHouse), RevitUtil.ConvertToFeet(gridLineExtension));
    }
}