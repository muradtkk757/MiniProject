using BusinessLogicLayer.Services.Contracts;
using DataAccessLayer;
using DataAccessLayer.Models;
using DataAccessLayer.Repostories.Contracts;
using DataAccessLayerModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BusinessLogicLayer.Services.Contracts
{
     public interface IBookService
    {
        void Create(Book book);
        Book Get(int id);
        List<Book> GetAll();
        void Update(Book book);
        void Delete(int id);
        List<Book> Search(string keyword);
    }
}