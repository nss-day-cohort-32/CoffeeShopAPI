﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using CoffeeShop.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CoffeeShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoffeesController : ControllerBase
    {
        private readonly IConfiguration _config;

        public CoffeesController(IConfiguration config)
        {
            _config = config;
        }

        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }

        [HttpGet]
        public ActionResult<List<Coffee>> Get([FromQuery] string beanType)
        {
            if (beanType == null)
            {
                beanType = "";
            }

            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Id, Title, BeanType 
                        FROM Coffee
                        WHERE BeanType LIKE '%' + @beanType + '%'";
                        
                    cmd.Parameters.Add(new SqlParameter("@beanType", beanType));
                    SqlDataReader reader = cmd.ExecuteReader();
                    List<Coffee> coffees = new List<Coffee>();

                    while (reader.Read())
                    {
                        Coffee coffee = new Coffee
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Title = reader.GetString(reader.GetOrdinal("Title")),
                            BeanType = reader.GetString(reader.GetOrdinal("BeanType"))
                        };

                        coffees.Add(coffee);
                    }
                    reader.Close();

                    return Ok(coffees);
                }
            }
        }

        [HttpGet("{id}", Name = "GetCoffee")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT
                            Id, Title, BeanType
                        FROM Coffee
                        WHERE Id = @coffeeid";
                    cmd.Parameters.Add(new SqlParameter("@coffeeid", id));
                    SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    Coffee coffee = null;

                    if (await reader.ReadAsync())
                    {
                        coffee = new Coffee
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Title = reader.GetString(reader.GetOrdinal("Title")),
                            BeanType = reader.GetString(reader.GetOrdinal("BeanType"))
                        };
                    }

                    if (coffee == null)
                    {
                        return NotFound($"Coffee with the id {id} was not found");
                    }

                    reader.Close();

                    return Ok(coffee);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Coffee coffee)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Coffee (Title, BeanType)
                                        OUTPUT INSERTED.Id
                                        VALUES (@title, @beanType)";
                    cmd.Parameters.Add(new SqlParameter("@title", coffee.Title));
                    cmd.Parameters.Add(new SqlParameter("@beanType", coffee.BeanType));

                    var result = await cmd.ExecuteScalarAsync();
                    var newId = (int)result;
                    coffee.Id = newId;
                    return CreatedAtRoute("GetCoffee", new { id = newId }, coffee);
                }
            }
        }
    }
}