namespace Simpleform.buidhouse.models;

//Models sẽ dùng đơn vị feet - DTO có mapper sẽ config từ đơn vị mm người dùng sang feet
public record GridConfig
{
    public double WidthHouse {get; init;}
    public double LengthHouse {get; init;}

    public double gridLineExtension {get; init;}

    public GridConfig(double WidthHouse, double LengthHouse,double gridLineExtension  )
    {
        this.WidthHouse = WidthHouse;
        this.LengthHouse = LengthHouse;
        this.gridLineExtension = gridLineExtension;
    }

}