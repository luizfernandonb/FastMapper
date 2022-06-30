using System.Diagnostics;
using AutoMapper;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Lokad.ILPack;
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

        while (true) ;
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

        FastMapper.Bind<Car, CarDTO>((config) =>
        {
            config.InstancesArraySize = 16;
        });

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
    public CarDTO FastMapperr()
    {
        var carDto = _car.MapTo<CarDTO>();

#if DEBUG
        var asmGen = new AssemblyGenerator();
        asmGen.GenerateAssembly(FastMapper.GetAssemblyOfCreatedType(), @"C:\Users\luizf\Desktop\assembly.dll");
#endif

        Debug.Assert(carDto.Model == "Ferrari");
        Debug.Assert(carDto.Engine == "V8");
        Debug.Assert(carDto.Year == DateTime.Now.Year);

        return carDto;
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
