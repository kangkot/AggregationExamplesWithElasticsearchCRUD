﻿using System;
using System.Collections.Generic;
using AggregationsWithElasticsearchCrud.Model;
using ElasticsearchCRUD;
using ElasticsearchCRUD.ContextSearch.SearchModel;
using ElasticsearchCRUD.ContextSearch.SearchModel.AggModel;
using ElasticsearchCRUD.Model.SearchModel;
using ElasticsearchCRUD.Model.SearchModel.Aggregations;

namespace AggregationsWithElasticsearchCrud
{
	public class ElasticsearchProvider
	{
		protected readonly IElasticsearchMappingResolver ElasticsearchMappingResolver = new ElasticsearchMappingResolver();
		protected const string ConnectionString = "http://localhost:9200";

		public TermsBucketAggregationsResult SearchFirstNameTermsBucketAggregation()
		{
			TermsBucketAggregationsResult aggResult;
			var search = new Search
			{
				Aggs = new List<IAggs>
				{
					new TermsBucketAggregation("testFirstName", "firstname")
					{
						Size = 20
					}
				}
			};

			using (var context = new ElasticsearchContext(ConnectionString, ElasticsearchMappingResolver))
			{
				var items = context.Search<Person>(search, new SearchUrlParameters { SeachType = SeachType.count });
				aggResult = items.PayloadResult.Aggregations.GetComplexValue<TermsBucketAggregationsResult>("testFirstName");
			}

			return aggResult;
		}

		/// <summary>
		/// Get the five most popular lastnames groups with the person data
		/// </summary>
		/// <returns></returns>
		public TermsBucketAggregationsResult SearchLastNameTermsBucketAggregations()
		{
			TermsBucketAggregationsResult aggResult;
			var search = new Search
			{
				Aggs = new List<IAggs>
				{
					new TermsBucketAggregation("testLastName", "lastname")
					{
						Size = 5,
						Aggs = new List<IAggs>
						{
							new TopHitsMetricAggregation("tophits")
							{
								Size = 2
							}
						}
					}
				}
			};

			using (var context = new ElasticsearchContext(ConnectionString, ElasticsearchMappingResolver))
			{
				var items = context.Search<Person>(search, new SearchUrlParameters { SeachType = SeachType.count });
				aggResult = items.PayloadResult.Aggregations.GetComplexValue<TermsBucketAggregationsResult>("testLastName");
			}

			return aggResult;
		}

		public void SearchLastNameTermsBucketAggregationSignificantTermsBucketAggregationFirstNameMatches()
		{
			// Create the aggregation search result
			TermsBucketAggregationsResult aggResult;
			var search = new Search
			{
				Aggs = new List<IAggs>
				{
					new TermsBucketAggregation("testLastName", "lastname")
					{
						Size = 0,
						Aggs = new List<IAggs>
						{
							new SignificantTermsBucketAggregation("testFirstName", "firstname")
							{
								Size = 20,
								Aggs = new List<IAggs>
								{
									new TopHitsMetricAggregation("tophits")
									{
										Size = 20
									}
								}
							}
						}
					}
				}
			};

			// Send the search to Elasticsearch
			using (var context = new ElasticsearchContext(ConnectionString, ElasticsearchMappingResolver))
			{
				var items = context.Search<Person>(search, new SearchUrlParameters { SeachType = SeachType.count });
				aggResult = items.PayloadResult.Aggregations.GetComplexValue<TermsBucketAggregationsResult>("testLastName");
			}

			// Lets display the Aggregation results in the console
			foreach (var bucket in aggResult.Buckets)
			{
				var significantTermsBucketAggregationsResult = bucket.GetSubAggregationsFromJTokenName<SignificantTermsBucketAggregationsResult>("testFirstName");

				foreach (var childbucket in significantTermsBucketAggregationsResult.Buckets)
				{
					bool writeHeader = true;
					var hits = childbucket.GetSubAggregationsFromJTokenName<TopHitsMetricAggregationsResult<Person>>("tophits");
					foreach (var hit in hits.Hits.HitsResult)
					{
						if (writeHeader)
						{
							Console.Write("\n{0} {1}, Found Ids: ", hit.Source.FirstName, hit.Source.LastName);
						}
						Console.Write("{0} ", hit.Id);
						writeHeader = false;
					}
				}
			}
		}

	}
}
