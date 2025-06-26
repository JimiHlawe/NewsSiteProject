// Services/ArticleService.cs
using NewsSite1.Models;
using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace NewsSite1.Services
{
    public class ArticleService
    {
        private readonly string _connectionString;

        public ArticleService(string connectionString)
        {
            _connectionString = connectionString;
        }

        private SqlConnection Connect()
        {
            SqlConnection con = new SqlConnection(_connectionString);
            con.Open();
            return con;
        }

        public List<Article> GetAllArticles()
        {
            List<Article> list = new List<Article>();
            using (SqlConnection con = Connect())
            {
                SqlCommand cmd = new SqlCommand("SELECT id, title, content FROM News_Articles", con);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(new Article
                    {
                        Id = Convert.ToInt32(dr["id"]),
                        Title = dr["title"].ToString(),
                        Content = dr["content"].ToString()
                    });
                }
            }
            return list;
        }


        public int SaveArticleAndGetId(Article article)
        {
            int articleId = 0;

            using (SqlConnection con = Connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_AddOrGetArticle", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@title", article.Title ?? "");
                cmd.Parameters.AddWithValue("@description", article.Description ?? "");
                cmd.Parameters.AddWithValue("@content", article.Content ?? "");
                cmd.Parameters.AddWithValue("@author", article.Author ?? "");
                cmd.Parameters.AddWithValue("@url", article.SourceUrl ?? "");
                cmd.Parameters.AddWithValue("@imageUrl", article.ImageUrl ?? "");
                cmd.Parameters.AddWithValue("@publishedAt", article.PublishedAt);

                SqlParameter outId = new SqlParameter("@articleId", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(outId);

                cmd.ExecuteNonQuery();
                articleId = (int)outId.Value;
            }

            return articleId;
        }
    }
}