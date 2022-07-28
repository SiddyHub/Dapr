﻿using AutoMapper;
using Dapr.Client;
using GloboTicket.Services.EventCatalog.Messages;
using GloboTicket.Services.EventCatalog.Models;
using GloboTicket.Services.EventCatalog.Repositories;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GloboTicket.Services.EventCatalog.Controllers
{
    [Route("api/events")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly IEventRepository eventRepository;
        private readonly IMapper mapper;
        private readonly DaprClient daprClient;

        public EventController(IEventRepository eventRepository, IMapper mapper, Dapr.Client.DaprClient daprClient)
        {
            this.eventRepository = eventRepository;
            this.mapper = mapper;
            this.daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Models.EventDto>>> Get(
            [FromQuery] Guid categoryId)
        {
            var result = await eventRepository.GetEvents(categoryId);
            return Ok(mapper.Map<List<Models.EventDto>>(result));
        }

        [HttpGet("{eventId}")]
        public async Task<ActionResult<Models.EventDto>> GetById(Guid eventId)
        {
            var result = await eventRepository.GetEventById(eventId);
            return Ok(mapper.Map<Models.EventDto>(result));
        }

        [HttpPost("eventpriceupdate")]
        public async Task<ActionResult<PriceUpdate>> Post(PriceUpdate priceUpdate)
        {
            var eventToUpdate = await eventRepository.GetEventById(priceUpdate.EventId);
            eventToUpdate.Price = priceUpdate.Price;
            await eventRepository.SaveChanges();

            //send integration event on to service bus

            PriceUpdatedMessage priceUpdatedMessage = new PriceUpdatedMessage
            {
                EventId = priceUpdate.EventId,
                Price = priceUpdate.Price
            };

            try
            {
                //await messageBus.PublishMessage(priceUpdatedMessage, "priceupdatedmessage");
                await daprClient.PublishEventAsync("pubsub", "priceupdatedmessage", priceUpdatedMessage);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }


            return Ok(priceUpdate);
        }
    }
}