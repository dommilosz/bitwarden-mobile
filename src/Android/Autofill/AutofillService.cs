﻿using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Service.Autofill;
using Android.Views;
using Bit.App.Abstractions;
using System.Collections.Generic;
using System.Linq;
using XLabs.Ioc;

namespace Bit.Android.Autofill
{
    [Service(Permission = Manifest.Permission.BindAutofillService, Label = "bitwarden")]
    [IntentFilter(new string[] { "android.service.autofill.AutofillService" })]
    [MetaData("android.autofill", Resource = "@xml/autofillservice")]
    [Register("com.x8bit.bitwarden.Autofill.AutofillService")]
    public class AutofillService : global::Android.Service.Autofill.AutofillService
    {
        private ICipherService _cipherService;
        private ILockService _lockService;

        public async override void OnFillRequest(FillRequest request, CancellationSignal cancellationSignal, FillCallback callback)
        {
            var structure = request.FillContexts?.LastOrDefault()?.Structure;
            if(structure == null)
            {
                return;
            }

            var parser = new Parser(structure);
            parser.ParseForFill();

            if(!parser.FieldCollection.Fields.Any() || string.IsNullOrWhiteSpace(parser.Uri))
            {
                return;
            }

            if(_lockService == null)
            {
                _lockService = Resolver.Resolve<ILockService>();
            }

            if(true) // if locked
            {
                var authResponse = AutofillHelpers.BuildAuthResponse(this, parser.FieldCollection);
                callback.OnSuccess(authResponse);
                return;
            }

            if(_cipherService == null)
            {
                _cipherService = Resolver.Resolve<ICipherService>();
            }

            // build response
            var items = await AutofillHelpers.GetFillItemsAsync(_cipherService, parser.Uri);
            if(!items.Any())
            {
                return;
            }

            var response = AutofillHelpers.BuildFillResponse(this, parser.FieldCollection, items);
            callback.OnSuccess(response);
        }

        public override void OnSaveRequest(SaveRequest request, SaveCallback callback)
        {
            var structure = request.FillContexts?.LastOrDefault()?.Structure;
            if(structure == null)
            {
                return;
            }

            var clientState = request.ClientState;

            var parser = new Parser(structure);
            parser.ParseForSave();
            var filledAutofillFieldCollection = parser.FilledFieldCollection;
            //SaveFilledAutofillFieldCollection(filledAutofillFieldCollection);
        }
    }
}
