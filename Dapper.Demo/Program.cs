﻿using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Dapper.Demo.Entites;
using Dapper.Linq;

namespace Dapper.Demo
{
    public class newProducts {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //var list = new List<Products>(); 


            using (var db = new DapperDbContext("Group_Set"))
            {
                //LinqTestcs.Create(db);
                for (var i = 0; i < 100; i++)
                {
                    //TimeTest.Init(db);
                   
                }
                LinqTestcs.Create(db);
            }
            Console.ReadKey();
        }

    }

}
