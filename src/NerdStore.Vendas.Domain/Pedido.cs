﻿using FluentValidation.Results;
using NerdStore.Core.DomainObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NerdStore.Vendas.Domain
{
    public class Pedido : Entity, IAggregateRoot
    {
        public Pedido(int codigo, Guid clienteId, Guid? voucherId, bool voucherUtilizado, decimal desconto, decimal valorTotal, DateTime dataCadastro, PedidoStatus pedidoStatus)
        {
            ClienteId = clienteId;
            VoucherUtilizado = voucherUtilizado;
            Desconto = desconto;
            ValorTotal = valorTotal;
            _pedidoItens = new List<PedidoItem>();
        }

        protected Pedido()
        {
            _pedidoItens = new List<PedidoItem>();
        }

        private readonly List<PedidoItem> _pedidoItens;

        public int Codigo { get; private set; }
        public Guid ClienteId { get; private set; }
        public Guid? VoucherId { get; private set; }
        public bool VoucherUtilizado { get; private set; }
        public decimal Desconto { get; private set; }
        public decimal ValorTotal { get; private set; }
        public DateTime DataCadastro { get; private set; }
        public PedidoStatus PedidoStatus { get; private set; }

        public IReadOnlyCollection<PedidoItem> PedidoItens => _pedidoItens;

        public Voucher Voucher { get; private set; }

        public void TornarRascunho()
        {
            PedidoStatus = PedidoStatus.Rascunho;
        }

        public void IniciarPedido()
        {
            PedidoStatus = PedidoStatus.Iniciado;
        }

        public void FinalizarPedido()
        {
            PedidoStatus = PedidoStatus.Pago;
        }

        public void CancelarPedido()
        {
            PedidoStatus = PedidoStatus.Cancelado;
        }

        public void AtualizarUnidades(PedidoItem item, int unidades)
        {
            item.AtualizarUnidades(unidades);
            AtualizarItem(item);
        }

        public void AtualizarItem(PedidoItem item)
        {
            if (!item.EhValido()) return;

            item.AssociarPedido(Id);

            var itemExistente = _pedidoItens.FirstOrDefault(p => p.ProdutoId == item.ProdutoId);

            if (itemExistente == null) throw new DomainException("O item não pertence ao pedido.");

            _pedidoItens.Remove(itemExistente);

            _pedidoItens.Add(item);

            CalcularValorPedido();
        }

        public void RemoverItem(PedidoItem item)
        {
            if (!item.EhValido()) return;

            var itemExistente = _pedidoItens.FirstOrDefault(p => p.ProdutoId == item.ProdutoId);

            if (itemExistente == null) throw new DomainException("O item não pertence ao pedido.");

            _pedidoItens.Remove(itemExistente);

            CalcularValorPedido();
        }

        public void AdicionarItem(PedidoItem item)
        {
            if (!item.EhValido()) return;

            item.AssociarPedido(Id);

            if (PedidoItemExistente(item))
            {
                var itemExistente = _pedidoItens.FirstOrDefault(p => p.ProdutoId == item.ProdutoId);
                itemExistente.AdicionarUnidades(item.Quantidade);
                item = itemExistente;

                _pedidoItens.Remove(itemExistente);
            }

            item.CalcularValor();
            _pedidoItens.Add(item);

            CalcularValorPedido();
        }

        public bool PedidoItemExistente(PedidoItem item)
        {
            return _pedidoItens.Any(p => p.ProdutoId == item.ProdutoId);
        }

        public ValidationResult AplicarVoucher(Voucher voucher)
        {
            var validationResult = voucher.ValidarSeAplicavel();

            if (!validationResult.IsValid) return validationResult;

            Voucher = voucher;
            VoucherUtilizado = true;
            CalcularValorPedido();

            return validationResult;
        }

        public void CalcularValorPedido()
        {
            ValorTotal = PedidoItens.Sum(p => p.CalcularValor());
            CalcularValorTotalDesconto();
        }

        public void CalcularValorTotalDesconto()
        {
            if (!VoucherUtilizado) return;

            var desconto = decimal.Zero;
            var valor = ValorTotal;

            if (Voucher.TipoDescontoVoucher == TipoDescontoVoucher.Porcentagem)
            {
                if (Voucher.Percentual.HasValue)
                {
                    desconto = (valor * Voucher.Percentual.Value) / 100;
                    valor -= desconto;
                }
            }
            else
            {
                if (Voucher.ValorDesconto.HasValue)
                {
                    desconto = Voucher.ValorDesconto.Value;
                    valor -= desconto;
                }
            }

            ValorTotal = valor < 0 ? 0 : valor;
            Desconto = desconto;
        }

        public static class PedidoFactory
        {
            public static Pedido NovoPedidoRascunho(Guid clienteId)
            {
                var pedido = new Pedido
                {
                    ClienteId = clienteId
                };

                pedido.TornarRascunho();

                return pedido;
            }
        }
    }
}
