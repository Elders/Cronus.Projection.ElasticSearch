//using System;
//using System.Collections.Generic;
//using System.Linq.Expressions;
//using System.Runtime.Serialization;
//using Elders.Cronus.DomainModeling;

//namespace Elders.Cronus.Projections.ElasticSearch.GG
//{
//    public class ProjectionMeta
//    {
//        public string Name { get; set; }

//        public List<Type> Events { get; set; }
//    }

//    public interface IProjectionMetaReader
//    {
//        List<ProjectionMeta> Read();
//    }

//    public interface IProjectionExecutor<TState>
//    {
//        TState Execute(ProjectionMeta meta);
//    }







//    public class ApiContorlerr
//    {

//        public void Get()
//        {
//            ProjectionsRepo.Load<Proj>()
//        }
//    }











//    public class Evnt1 : IEvent { public string id; }
//    public class Evnt2 : IEvent { }

//    [DataContract(Name = "2cf92746-a68b-445b-8a97-213a579d4669")]
//    public class Proj : IProjection,
//        IEventHandler<Evnt1>,
//        IEventHandler<Evnt2>
//    {
//        public void Handle(Evnt2 @event)
//        {
//            throw new NotImplementedException();
//        }

//        public void Handle(Evnt1 @event)
//        {
//            throw new NotImplementedException();
//        }
//    }

//    [DataContract(Name = "2cf92746-a68b-445b-8a97-213a579d4669")]
//    public class Proj2 : IProjection,
//        IEventHandler<Evnt1>,
//        IEventHandler<Evnt2>
//    {
//        public void Handle(Evnt2 @event)
//        {
//        }

//        public void Handle(Evnt1 @event)
//        {

//        }
//    }

//    public interface IProjec { };
//    public delegate object StreamId<T>(T evnet);
//    public delegate TState When<TEvent, TState>(TEvent evnet, TState state);


//    public class Proj3 : IProjec
//    {
//        public StreamId<Evnt1> id = e => { e.PropertyId};

//        When<Evnt1, List<Acti>> event1 = (x, y) => { string st = ""; st = x.id; return st; };
//    }

//}
