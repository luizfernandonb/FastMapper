using AutoMapper;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using LuizStudios.Mapper.TestsConsole.Classes;
using Mapster;
using Nelibur.ObjectMapper;

namespace LuizStudios.FastMapper.TestsConsole;

public static class Program
{
    public static void Main()
    {
#if RELEASE
        BenchmarkRunner.Run<Benchmarks>();
#else
        BenchmarkRunner.Run<Benchmarks>(new DebugInProcessConfig());
#endif

        Console.ReadKey();
    }
}

[MemoryDiagnoser]
public class Benchmarks
{
    private Car? _car;
    //private CarDTO? _carDTO;

#if RELEASE
    private IMapper? _mapper;
#endif

    [GlobalSetup]
    public void Setup()
    {
        _car = new Car();

        //_carDTO = new CarDTO();
        //FastMapper.Bind<CarDTO, Car>();

        FastMapper.Bind<Car, CarDTO>();

#if RELEASE
        TinyMapper.Bind<Car, CarDTO>();

        var cfg = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Car, CarDTO>();
        });
        _mapper = cfg.CreateMapper();
#endif
    }

    [Benchmark]
    public CarDTO FastMapperBenchmark()
    {
        return _car.MapTo<CarDTO>();
    }

#if RELEASE
    [Benchmark]
    public CarDTO TinyMapperr()
    {
        return TinyMapper.Map<CarDTO>(_car);
    }

    [Benchmark]
    public CarDTO AutoMapperr()
    {
        return _mapper!.Map<CarDTO>(_car);
    }

    [Benchmark]
    public CarDTO Mappsterr()
    {
        return _car!.Adapt<CarDTO>();
    }
#endif

    /*public static ProdutoDTO ProdutoToProdutoDTO(Produto produto)
    {
        return new ProdutoDTO
        {
            LojaId = produto.LojaId,
            Nome = produto.Nome,
            Preco = produto.Preco,
            PrecoVarejo = produto.PrecoVarejo
        };
    }*/
}
