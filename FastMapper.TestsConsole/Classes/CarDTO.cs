using LuizStudios.src.Attributes;

namespace LuizStudios.Mapper.TestsConsole.Classes;

public class CarDTO
{
    public int Year { get; set; }

    public string? Model { get; set; }

    [FastMapperForThis("V12")]
    public string? Engine { get; set; }
}
