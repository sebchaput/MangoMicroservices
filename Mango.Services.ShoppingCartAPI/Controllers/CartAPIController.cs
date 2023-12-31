using AutoMapper;
using Mango.MessageBus;
using Mango.Services.ShoppingCartAPI.Data;
using Mango.Services.ShoppingCartAPI.Models;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Mango.Services.ShoppingCartAPI.Service.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection.PortableExecutable;

namespace Mango.Services.ShoppingCartAPI.Controllers
{
    [Route("api/cart")]
    [ApiController]
    public class CartAPIController : ControllerBase
    {
        private readonly AppDbContext _db;
        private ResponseDto _response;
        private IMapper _mapper;
        private IProductService _productService;
        private ICouponService _couponService;
        private readonly IMessageBus _messageBus;
        private IConfiguration _configuration;

        public CartAPIController(
            AppDbContext db, 
            IMapper mapper, 
            IProductService productService, 
            ICouponService couponService,
            IMessageBus messageBus,
            IConfiguration configuration)
        {
            _db = db;
            _response = new ResponseDto();
            _mapper = mapper;
            _couponService = couponService;
            _productService = productService;
            _messageBus = messageBus;
            _configuration = configuration;
        }

        [HttpGet("GetCart/{userId}")]
        public async Task<ResponseDto> GetCart(string userId)
        {
            try
            {
                CartDto cartDto = new()
                {
                    CartHeader = _mapper.Map<CartHeaderDto>(_db.CartHeaders.First(u => u.UserId == userId))
                };

                cartDto.CartDetails = _mapper.Map<IEnumerable<CartDetailsDto>>(
                    _db.CartDetails.Where(u => u.CartHeaderId == cartDto.CartHeader.CartHeaderId));

                IEnumerable<ProductDto> productDtos = await _productService.GetProducts();

                foreach(var item in cartDto.CartDetails)
                {
                    item.Product = productDtos.FirstOrDefault(u => u.ProductId == item.ProductId);
                    cartDto.CartHeader.CartTotal += (item.Count * item.Product.Price);
                }

                if (!string.IsNullOrEmpty(cartDto.CartHeader.CouponCode))
                {
                    CouponDto couponDto = await _couponService.GetCoupon(cartDto.CartHeader.CouponCode);

                    // If we have a coupon
                    // and the cart total is greater or equal to coupon minimum amount,
                    // we have to apply discount
                    if (couponDto != null && cartDto.CartHeader.CartTotal >= couponDto.MinAmount)
                    {
                        cartDto.CartHeader.CartTotal -= couponDto.DiscountAmount;
                        cartDto.CartHeader.Discount = couponDto.DiscountAmount;
                    }
                }

                _response.Result = cartDto;
            }
            catch(Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [HttpPost("ApplyCoupon")]
        public async Task<ResponseDto> ApplyCoupon([FromBody] CartDto cartDto)
        {
            try
            {
                var cartHeaderFromDb = await _db.CartHeaders.FirstAsync(
                    u => u.UserId == cartDto.CartHeader.UserId);

                cartHeaderFromDb.CouponCode = cartDto.CartHeader.CouponCode;

                _db.CartHeaders.Update(cartHeaderFromDb);
                _db.SaveChanges();

                _response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [HttpPost("EmailCartRequest")]
        public async Task<ResponseDto> EmailCartRequest([FromBody] CartDto cartDto)
        {
            try
            {
                await _messageBus.PublishMessage(cartDto, _configuration.GetValue<string>("TopicAndQueueNames:EmailShoppingCartQueue"));
                _response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [HttpPost("CartUpsert")]
        public async Task<ResponseDto> CartUpsert(CartDto cartDto)
        {
            try
            {
                var cartHeaderFromDb = await _db.CartHeaders.FirstOrDefaultAsync(
                    u => u.UserId == cartDto.CartHeader.UserId);
                if (cartHeaderFromDb == null)
                {
                    // Cart does not exist - create a cart with his product
                    CartHeader cartHeader = _mapper.Map<CartHeader>(cartDto.CartHeader);
                    _db.CartHeaders.Add(cartHeader);
                    await _db.SaveChangesAsync();

                    cartDto.CartDetails.First().CartHeaderId = cartHeader.CartHeaderId;
                    _db.CartDetails.Add(_mapper.Map<CartDetails>(cartDto.CartDetails.First()));
                    await _db.SaveChangesAsync();
                }
                else
                {
                    // Cart exists - check if product exist
                    var cartDetailFromDb = await _db.CartDetails.FirstOrDefaultAsync(
                        u => u.CartHeaderId == cartHeaderFromDb.CartHeaderId &&
                        u.ProductId == cartDto.CartDetails.First().ProductId);
                    if (cartDetailFromDb == null)
                    {
                        // Product does not exist - add product to cart
                        cartDto.CartDetails.First().CartHeaderId = cartHeaderFromDb.CartHeaderId;
                        _db.CartDetails.Add(_mapper.Map<CartDetails>(cartDto.CartDetails.First()));
                        await _db.SaveChangesAsync();
                    }
                    else
                    {
                        // Product exists for that cart - add count
                        cartDetailFromDb.Count += cartDto.CartDetails.First().Count;
                        _db.CartDetails.Update(cartDetailFromDb);
                        _db.SaveChanges();
                    }
                }
                _response.Result = cartDto;
            }
            catch (Exception ex)
            {
                _response.Message = ex.Message;
                _response.IsSuccess = false;
            }
            return _response;
        }

        [HttpPost("RemoveCart")]
        public async Task<ResponseDto> RemoveCart([FromBody]int cartDetailsId)
        {
            try
            {
                CartDetails cartDetailsFromDb = await _db.CartDetails.FirstAsync(
                    u => u.CartDetailsId == cartDetailsId);

                if (cartDetailsFromDb != null)
                {
                    int totalCountofCartItem = _db.CartDetails.Where(
                        u => u.CartHeaderId == cartDetailsFromDb.CartHeaderId).Count();

                    _db.CartDetails.Remove(cartDetailsFromDb);

                    if (totalCountofCartItem == 1)
                    {
                        CartHeader cartHeaderToRemove = await _db.CartHeaders.FirstOrDefaultAsync(
                            u => u.CartHeaderId == cartDetailsFromDb.CartHeaderId);
                        _db.CartHeaders.Remove(cartHeaderToRemove);
                    }
                    await _db.SaveChangesAsync();
                }
                else
                {
                    _response.IsSuccess = false;
                    _response.Message = "Cart Details Not Found";
                }
            }
            catch (Exception ex)
            {
                _response.Message = ex.Message;
                _response.IsSuccess = false;
            }
            return _response;
        }

    }
}
