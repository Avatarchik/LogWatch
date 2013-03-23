﻿using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LogWatch.Annotations;
using LogWatch.Messages;
using LogWatch.Sources;

namespace LogWatch.Features.Records {
    public class RecordsViewModel : ViewModelBase {
        private bool autoScroll;
        private RecordCollection records;

        public RecordsViewModel() {
            if (this.IsInDesignMode)
                return;

            this.Scheduler = System.Reactive.Concurrency.Scheduler.Default;
            this.SelectRecordCommand = new RelayCommand<Record>(this.SelectRecord);
            this.MessengerInstance.Register<NavigatedToRecordMessage>(this, this.OnNavigateToRecord);
        }

        public RecordCollection Records {
            get { return this.records; }
            private set { this.Set(ref this.records, value); }
        }

        public bool AutoScroll {
            get { return this.autoScroll; }
            set { this.Set(ref this.autoScroll, value); }
        }

        public RelayCommand<Record> SelectRecordCommand { get; set; }

        public IObservable<VisibleItemsInfo> RecordContext {
            set {
                if (value == null)
                    return;

                value.Where(x => x.FirstItem != null && x.LastItem != null)
                     .Sample(TimeSpan.FromMilliseconds(300), this.Scheduler)
                     .ObserveOn(new SynchronizationContextScheduler(SynchronizationContext.Current))
                     .Subscribe(
                         info =>
                         this.MessengerInstance.Send(
                             new RecordContextChangedMessage(
                             (Record) info.FirstItem,
                             (Record) info.LastItem)));
            }
        }

        public IScheduler Scheduler { get; set; }
        public LogSourceInfo LogSourceInfo { get; set; }

        private void Set<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null) {
            this.Set(propertyName, ref field, newValue, false);
        }

        private void SelectRecord(Record record) {
            this.MessengerInstance.Send(new RecordSelectedMessage(record));
        }

        public void Initialize() {
            this.Records = new RecordCollection(this.LogSourceInfo.Source) {Scheduler = this.Scheduler};
            this.Records.Initialize();
            this.AutoScroll = this.LogSourceInfo.AutoScroll;
        }

        private void OnNavigateToRecord(NavigatedToRecordMessage message) {
            this.AutoScroll = false;
            this.Navigated(this, new GoToIndexEventArgs(message.Index));
        }

        [UsedImplicitly]
        public event EventHandler Navigated = (sender, args) => { };
    }
}