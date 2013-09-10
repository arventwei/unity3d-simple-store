#import <store.h>

void Store_init(unsigned char* productsId) 
{
	NSLog(@"Store_init");
	
	NSArray *productsIdArray = [productsId componentsSeparatedByString:@";"];
	NSSet *productsIdSet = [NSSet setWithArray:productsIdArray];
		
}

bool Store_canMakePayments()
{
	NSLog(@"Store_canMakePayments");
	return [SKPaymentQueue canMakePayments];
}
	
void Store_purchase(unsigned char* productId)
{
	NSString *productIdStr = [NSString stringWithUTF8String:productId];
	NSLog(@"Store_purchase productId '%@'", productIdStr);
}
