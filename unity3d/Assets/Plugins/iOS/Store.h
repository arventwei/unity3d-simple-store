#ifndef Store_h
#define Store_h

#import <StoreKit/StoreKit.h>

extern "C" {
	void Store_init(unsigned char* productsId);
	// this must be called before init to check if can initialize
	bool Store_canMakePayments();
	void Store_purchase(unsigned char* productId);
}

#endif
