﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NerdStore.Core.Data.EventSourcing;
using System;
using System.Threading.Tasks;

namespace NerdStore.WebApp.Mvc.Controllers
{
    [Authorize]
    public class EventosController : Controller
    {
        private readonly IEventSourcingRepository _eventSourcingRepository;

        public EventosController(IEventSourcingRepository eventSourcingRepository)
        {
            _eventSourcingRepository = eventSourcingRepository;
        }

        [HttpGet("eventos/{id:guid}")]
        public async Task<IActionResult> Index(Guid id)
        {
            return View(await _eventSourcingRepository.ObterEventos(id));
        }
    }
}
